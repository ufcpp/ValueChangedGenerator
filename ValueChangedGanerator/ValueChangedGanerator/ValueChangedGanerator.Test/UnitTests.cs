using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.ComponentModel;
using TestHelper;

namespace ValueChangedGanerator.Test
{
    [TestClass]
    public class UnitTest : ConventionCodeFixVerifier
    {
        protected override IEnumerable<MetadataReference> References
        {
            get
            {
                foreach (var r in base.References) yield return r;
                yield return MetadataReference.CreateFromFile(typeof(INotifyPropertyChanged).Assembly.Location);
            }
        }

        protected override CSharpCompilationOptions CompilationOptions
            => base.CompilationOptions
            .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
            {
                { "CS0649", ReportDiagnostic.Suppress },
            });

        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TypicalUsage() => VerifyCSharpByConvention();

        [TestMethod]
        public void GenericType() => VerifyCSharpByConvention();

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new ValueChangedGaneratorCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ValueChangedGaneratorAnalyzer();
    }
}