using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ValueChangedGenerator
{
    public class Generator
    {
        public CompilationUnitSyntax GeneratePartialDeclaration(INamedTypeSymbol container, ClassDeclarationSyntax classDecl)
        {
            var strDecl = (StructDeclarationSyntax)classDecl.ChildNodes().First(x => x is StructDeclarationSyntax);

            var def = new RecordDefinition(strDecl);
            var generatedNodes = GetGeneratedNodes(def).ToArray();

            var newClassDecl = container.GetContainingTypesAndThis()
                .Select((type, i) => i == 0
                    ? ClassDeclaration(type.Name).GetPartialTypeDelaration().AddMembers(generatedNodes)
                    : ClassDeclaration(type.Name).GetPartialTypeDelaration())
                .Aggregate((a, b) => b.AddMembers(a));

            var ns = container.ContainingNamespace.FullName().NullIfEmpty();

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

        private UsingDirectiveSyntax[] WithComponentModel(IEnumerable<UsingDirectiveSyntax> usings)
        {
            const string SystemComponentModel = "System.ComponentModel";

            if (usings.Any(x => x.Name.WithoutTrivia().GetText().ToString() == SystemComponentModel))
                return usings.ToArray();

            return usings.Concat(new[] { UsingDirective(IdentifierName("System.ComponentModel")) }).ToArray();
        }

        private IEnumerable<MemberDeclarationSyntax> GetGeneratedNodes(RecordDefinition def)
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

        private IEnumerable<MemberDeclarationSyntax> WithTrivia(IEnumerable<MemberDeclarationSyntax> members, SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia)
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

        private string NameOf(SimpleProperty p) => NameOf(p.Name);
        private string NameOf(DependentProperty p) => NameOf(p.Name);
        private string NameOf(string identifier) => $"nameof({identifier})";

        private IEnumerable<MemberDeclarationSyntax> GetGeneratedMember(SimpleProperty p)
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

        private IEnumerable<MemberDeclarationSyntax> GetGeneratedMember(DependentProperty p)
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
