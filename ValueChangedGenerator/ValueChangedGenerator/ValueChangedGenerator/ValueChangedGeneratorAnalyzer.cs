using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValueChangedGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValueChangedGeneratorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ValueChangedGenerator";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzerSyntax, SyntaxKind.StructDeclaration);
        }

        private void AnalyzerSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not StructDeclarationSyntax s) return;

            var parent = s.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            if (parent is null) return;

            var name = s.Identifier.Text;
            if (name != "NotifyRecord") return;

            if (!parent.ChildNodes().Any(n => n == s))
                return;

            var diagnostic = Diagnostic.Create(Rule, s.GetLocation(), parent.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
