using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValueChangedGenerator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private sealed class SyntaxReceiver : ISyntaxReceiver
        {
            public List<StructDeclarationSyntax> CandidateStructs { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is not StructDeclarationSyntax structDeclarationSyntax)
                    return;

                var parent = structDeclarationSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                if (parent is null) return;

                var name = structDeclarationSyntax.Identifier.Text;
                if (name != "NotifyRecord") return;

                if (!parent.ChildNodes().Any(n => n == structDeclarationSyntax))
                    return;

                CandidateStructs.Add(structDeclarationSyntax);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            foreach (var candidateStruct in receiver.CandidateStructs)
            {
                if (candidateStruct.FirstAncestorOrSelf<ClassDeclarationSyntax>() is ClassDeclarationSyntax typeDecl)
                {
                    // Code from: https://github.com/YairHalberstadt/stronginject/blob/779a38e7e74b92c87c86ded5d1fef55744d34a83/StrongInject/Generator/SourceGenerator.cs#L87
                    static string GenerateHintName(INamedTypeSymbol container)
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.Append(container.ContainingNamespace.FullName());
                        foreach (var type in container.GetContainingTypesAndThis().Reverse())
                        {
                            stringBuilder.Append(".");
                            stringBuilder.Append(type.Name);
                            if (type.TypeParameters.Length > 0)
                            {
                                stringBuilder.Append("_");
                                stringBuilder.Append(type.TypeParameters.Length);
                            }
                        }
                        stringBuilder.Append(".g.cs");
                        return stringBuilder.ToString();
                    }
                    var model = context.Compilation.GetSemanticModel(typeDecl.SyntaxTree);
                    if (model.GetDeclaredSymbol(typeDecl) is INamedTypeSymbol type)
                    {
                        var generator = new Generator();
                        context.AddSource(GenerateHintName(type), generator.GeneratePartialDeclaration(type, typeDecl).ToFullString());
                    }
                }
            }
        }
    }
}
