using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
                        context.AddSource(GenerateHintName(type), GeneratePartialDeclaration(typeDecl).ToFullString());
                }
            }
        }

        private static CompilationUnitSyntax GeneratePartialDeclaration(ClassDeclarationSyntax classDecl)
        {
            var strDecl = (StructDeclarationSyntax)classDecl.ChildNodes().First(x => x is StructDeclarationSyntax);

            var def = new RecordDefinition(strDecl);
            var generatedNodes = GetGeneratedNodes(def).ToArray();

            var newClassDecl = classDecl.GetPartialTypeDelaration()
                .AddMembers(generatedNodes);

            var ns = classDecl.FirstAncestorOrSelf<NamespaceDeclarationSyntax>()?.Name.WithoutTrivia().GetText().ToString();

            MemberDeclarationSyntax topDecl;
            if (ns != null)
            {
                topDecl = NamespaceDeclaration(IdentifierName(ns))
                    .AddMembers(newClassDecl);
            }
            else
            {
                topDecl = newClassDecl;
            }

            var root = (CompilationUnitSyntax)classDecl.SyntaxTree.GetRoot();

            return CompilationUnit().AddUsings(WithComponentModel(root.Usings))
                .AddMembers(topDecl)
                .WithTrailingTrivia(CarriageReturnLineFeed)
                .NormalizeWhitespace();
        }

        private static UsingDirectiveSyntax[] WithComponentModel(IEnumerable<UsingDirectiveSyntax> usings)
        {
            const string SystemComponentModel = "System.ComponentModel";

            if (usings.Any(x => x.Name.WithoutTrivia().GetText().ToString() == SystemComponentModel))
                return usings.ToArray();

            return usings.Concat(new[] { UsingDirective(IdentifierName("System.ComponentModel")) }).ToArray();
        }

        private static IEnumerable<MemberDeclarationSyntax> GetGeneratedNodes(RecordDefinition def)
        {
            yield return CSharpSyntaxTree.ParseText(
                @"        private NotifyRecord _value;
")
                .GetRoot().ChildNodes()
                .OfType<MemberDeclarationSyntax>()
                .First()
                .WithTrailingTrivia(CarriageReturnLineFeed, CarriageReturnLineFeed);

            foreach (var p in def.Properties)
                foreach (var s in WithTrivia(GetGeneratedMember(p), p.LeadingTrivia, p.TrailingTrivia))
                    yield return s;

            foreach (var p in def.DependentProperties)
                foreach (var s in WithTrivia(GetGeneratedMember(p), p.LeadingTrivia, p.TrailingTrivia))
                    yield return s;
        }

        private static IEnumerable<MemberDeclarationSyntax> WithTrivia(IEnumerable<MemberDeclarationSyntax> members, SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia)
        {
            var array = members.ToArray();

            if (array.Length == 0) yield break;

            if (array.Length == 1)
            {
                yield return array[0]
                    .WithLeadingTrivia(leadingTrivia)
                    .WithTrailingTrivia(trailingTrivia);

                yield break;
            }

            yield return array[0].WithLeadingTrivia(leadingTrivia);

            for (int i = 1; i < array.Length - 1; i++)
                yield return array[i];

            yield return array[array.Length - 1].WithTrailingTrivia(trailingTrivia);
        }

        private static string NameOf(SimpleProperty p) => NameOf(p.Name);
        private static string NameOf(DependentProperty p) => NameOf(p.Name);
        private static string NameOf(string identifier) => $"nameof({identifier})";

        private static IEnumerable<MemberDeclarationSyntax> GetGeneratedMember(SimpleProperty p)
        {
            var dependentChanged = string.Join("", p.Dependents.Select(d => $" OnPropertyChanged({d.Name}Property);"));
            var source = string.Format(@"        public {1} {0} {{ get {{ return _value.{0}; }} set {{ SetProperty(ref _value.{0}, value, {0}Property); {2} }} }}
        private static readonly PropertyChangedEventArgs {0}Property = new PropertyChangedEventArgs(" + NameOf(p) + ");",
                p.Name, p.Type.WithoutTrivia().GetText().ToString(), dependentChanged);

            var generatedNodes = CSharpSyntaxTree.ParseText(source)
                .GetRoot().ChildNodes()
                .OfType<MemberDeclarationSyntax>()
                .ToArray();

            return generatedNodes;
        }

        private static IEnumerable<MemberDeclarationSyntax> GetGeneratedMember(DependentProperty p)
        {
            var source = string.Format(@"        public {1} {0} => _value.{0};
        private static readonly PropertyChangedEventArgs {0}Property = new PropertyChangedEventArgs(" + NameOf(p) + @");
",
                p.Name, p.Type.WithoutTrivia().GetText().ToString());

            var generatedNodes = CSharpSyntaxTree.ParseText(source)
                .GetRoot().ChildNodes()
                .OfType<MemberDeclarationSyntax>()
                .ToArray();

            return generatedNodes;
        }
    }
}
