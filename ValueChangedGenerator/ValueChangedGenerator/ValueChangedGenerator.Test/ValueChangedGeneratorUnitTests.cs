﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using VerifyCS = ValueChangedGenerator.Test.CSharpCodeFixVerifier<
    ValueChangedGenerator.ValueChangedGeneratorAnalyzer,
    ValueChangedGenerator.ValueChangedGeneratorCodeFixProvider>;

namespace ValueChangedGenerator.Test
{
    public class ValueChangedGeneratorUnitTest
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [Fact]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("ValueChangedGenerator").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [Fact]
        public void GeneratePartialDeclaration()
        {
            var expected = @"using System.ComponentModel;

partial class Point
{
    private NotifyRecord _value;
    public int X
    {
        get
        {
            return _value.X;
        }

        set
        {
            SetProperty(ref _value.X, value, XProperty);
            OnPropertyChanged(ZProperty);
        }
    }

    private static readonly PropertyChangedEventArgs XProperty = new PropertyChangedEventArgs(nameof(X));
    public int Y
    {
        get
        {
            return _value.Y;
        }

        set
        {
            SetProperty(ref _value.Y, value, YProperty);
            OnPropertyChanged(ZProperty);
        }
    }

    private static readonly PropertyChangedEventArgs YProperty = new PropertyChangedEventArgs(nameof(Y));
    /// <summary>
    /// Name.
    /// </summary>
    public string Name
    {
        get
        {
            return _value.Name;
        }

        set
        {
            SetProperty(ref _value.Name, value, NameProperty);
        }
    }

    private static readonly PropertyChangedEventArgs NameProperty = new PropertyChangedEventArgs(nameof(Name));
    public int Z => _value.Z;
    private static readonly PropertyChangedEventArgs ZProperty = new PropertyChangedEventArgs(nameof(Z));
}";
            var text = @"
partial class Point : BindableBase
{
    struct NotifyRecord
    {
        public int X;
        public int Y;
        public int Z => X * Y;

        /// <summary>
        /// Name.
        /// </summary>
        public string Name;
    }
}";
            var compilation = CSharpCompilation.Create(assemblyName: "test", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(text));
            var generator = new Generator();
            var syntaxTrees = compilation.SyntaxTrees;
            Assert.Single(syntaxTrees);
            var classDecl = (ClassDeclarationSyntax)syntaxTrees.First().GetCompilationUnitRoot().DescendantNodes().First(x => x is ClassDeclarationSyntax);
            var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var container = model.GetDeclaredSymbol(classDecl);
            Assert.NotNull(container);
            Assert.Equal(expected, actual: generator.GeneratePartialDeclaration(container!, classDecl).ToFullString());
        }

        [Fact]
        public void GeneratePartialDeclaration_NestedPartialClasses()
        {
            var expected = @"using System.ComponentModel;

partial class C1
{
    partial class C2
    {
        partial class C3
        {
            private NotifyRecord _value;
            public int X
            {
                get
                {
                    return _value.X;
                }

                set
                {
                    SetProperty(ref _value.X, value, XProperty);
                    OnPropertyChanged(ZProperty);
                }
            }

            private static readonly PropertyChangedEventArgs XProperty = new PropertyChangedEventArgs(nameof(X));
            public int Y
            {
                get
                {
                    return _value.Y;
                }

                set
                {
                    SetProperty(ref _value.Y, value, YProperty);
                    OnPropertyChanged(ZProperty);
                }
            }

            private static readonly PropertyChangedEventArgs YProperty = new PropertyChangedEventArgs(nameof(Y));
            /// <summary>
            /// Name.
            /// </summary>
            public string Name
            {
                get
                {
                    return _value.Name;
                }

                set
                {
                    SetProperty(ref _value.Name, value, NameProperty);
                }
            }

            private static readonly PropertyChangedEventArgs NameProperty = new PropertyChangedEventArgs(nameof(Name));
            public int Z => _value.Z;
            private static readonly PropertyChangedEventArgs ZProperty = new PropertyChangedEventArgs(nameof(Z));
        }
    }
}";
            var text = @"
public partial class C1 : BindableBase
{
    struct NotifyRecord
    {
        public int X;
        public int Y;
        public int Z => X * Y;

        /// <summary>
        /// Name.
        /// </summary>
        public string Name;
    }

    public partial class C2 : BindableBase
    {
        struct NotifyRecord
        {
            public int X;
            public int Y;
            public int Z => X * Y;

            /// <summary>
            /// Name.
            /// </summary>
            public string Name;
        }

        public partial class C3 : BindableBase
        {
            struct NotifyRecord
            {
                public int X;
                public int Y;
                public int Z => X * Y;

                /// <summary>
                /// Name.
                /// </summary>
                public string Name;
            }
        }
    }
}";
            var compilation = CSharpCompilation.Create(assemblyName: "test", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(text));
            var generator = new Generator();
            var syntaxTrees = compilation.SyntaxTrees;
            Assert.Single(syntaxTrees);
            var classDecl = (ClassDeclarationSyntax)syntaxTrees.First().GetCompilationUnitRoot()
                .DescendantNodes().First(x => x is ClassDeclarationSyntax)
                .DescendantNodes().First(x => x is ClassDeclarationSyntax)
                .DescendantNodes().First(x => x is ClassDeclarationSyntax);
            var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var container = model.GetDeclaredSymbol(classDecl);
            Assert.NotNull(container);
            Assert.Equal(expected, actual: generator.GeneratePartialDeclaration(container!, classDecl).ToFullString());
        }
    }
}
