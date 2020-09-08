using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ValueChangedGenerator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueChangedGeneratorCodeFixProvider)), Shared]
    public class ValueChangedGeneratorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ValueChangedGeneratorAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedSolution: c => GenerateValueChanged(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Solution> GenerateValueChanged(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
        {
            document = await AddPartialModifier(document, classDecl, cancellationToken);
            document = await AddNewDocument(document, classDecl, cancellationToken);
            return document.Project.Solution;
        }

        private static async Task<Document> AddPartialModifier(Document document, ClassDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var newTypeDecl = typeDecl.AddPartialModifier();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var newRoolt = root.ReplaceNode(typeDecl, newTypeDecl)
                .WithAdditionalAnnotations(Formatter.Annotation);

            document = document.WithSyntaxRoot(newRoolt);
            return document;
        }

        private static async Task<Document> AddNewDocument(Document document, ClassDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var newRoot = await GeneratePartialDeclaration(document, typeDecl, cancellationToken);

            var name = typeDecl.Identifier.Text;
            var generatedName = name + ".ValueChanged.cs";

            var project = document.Project;

            var existed = project.Documents.FirstOrDefault(d => d.Name == generatedName);
            if (existed != null) return existed.WithSyntaxRoot(newRoot);
            else return project.AddDocument(generatedName, newRoot, document.Folders);
        }

        private static async Task<CompilationUnitSyntax> GeneratePartialDeclaration(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
        {
            var strDecl = (StructDeclarationSyntax)classDecl.ChildNodes().First(x => x is StructDeclarationSyntax);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var ti = semanticModel.GetTypeInfo(strDecl);

            var def = new RecordDefinition(strDecl);
            var generatedNodes = GetGeneratedNodes(def).ToArray();

            var newClassDecl = classDecl.GetPartialTypeDelaration()
                .AddMembers(generatedNodes)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var ns = classDecl.FirstAncestorOrSelf<NamespaceDeclarationSyntax>()?.Name.WithoutTrivia().GetText().ToString();

            MemberDeclarationSyntax topDecl;
            if (ns != null)
            {
                topDecl = NamespaceDeclaration(IdentifierName(ns))
                    .AddMembers(newClassDecl)
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }
            else
            {
                topDecl = newClassDecl;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;

            return CompilationUnit().AddUsings(WithComponentModel(root.Usings))
                .AddMembers(topDecl)
                .WithTrailingTrivia(CarriageReturnLineFeed)
                .WithAdditionalAnnotations(Formatter.Annotation);
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
                .WithTrailingTrivia(CarriageReturnLineFeed, CarriageReturnLineFeed)
                .WithAdditionalAnnotations(Formatter.Annotation)
                ;

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
