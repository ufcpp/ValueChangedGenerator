using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace ValueChangedGenerator.Test
{
    public class ValueChangedGeneratorUnitTest
    {
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

        [Fact]
        public void GeneratePartialDeclaration_NestedNamespaces()
        {
            var expected = @"using System.ComponentModel;

namespace N1.N2.N3
{
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
    }
}";
            var text = @"
namespace N1
{
    namespace N2
    {
        namespace N3
        {
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

        [Fact]
        public void GenerateUsings()
        {
            var expected = @"using System;
using System.Linq;
using System.ComponentModel;

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
        }
    }

    private static readonly PropertyChangedEventArgs XProperty = new PropertyChangedEventArgs(nameof(X));
}";
            var text = @"using System;
using System.Linq;

partial class Point : BindableBase
{
    struct NotifyRecord
    {
        public int X;
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
        public void GenerateFileScopedNamespace()
        {
            var expected = @"using System;
using System.ComponentModel;

namespace N
{
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
            }
        }

        private static readonly PropertyChangedEventArgs XProperty = new PropertyChangedEventArgs(nameof(X));
    }
}";
            var text = @"using System;
namespace N;

partial class Point : BindableBase
{
    struct NotifyRecord
    {
        public int X;
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
    }
}
