using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValueChangedGanerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValueChangedGaneratorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ValueChangedGanerator";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        internal const string Category = "Refactoring";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzerSyntax, SyntaxKind.StructDeclaration);
        }

        private void AnalyzerSyntax(SyntaxNodeAnalysisContext context)
        {
            var s = context.Node as StructDeclarationSyntax;
            var parent = s.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            if (parent == null) return;

            var name = s.Identifier.Text;
            if (name != "NotifyRecord") return;

            if (!parent.ChildNodes().Any(n => n == s))
                return;

            var diagnostic = Diagnostic.Create(Rule, parent.GetLocation(), parent.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
