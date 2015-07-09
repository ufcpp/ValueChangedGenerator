using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValueChangedGanerator.DataModels
{
    public class CodeGenerationOptions
    {
        public bool IsCsharp6 { get; }

        /// <summary>Record Constructor</summary>
        /// <param name="isCsharp6"><see cref="IsCsharp6"/></param>
        public CodeGenerationOptions(bool isCsharp6 = default(bool))
        {
            IsCsharp6 = isCsharp6;
        }
    }

    public class RecordDefinition
    {
        public IReadOnlyList<SimpleProperty> Properties { get; }

        public IReadOnlyList<DependentProperty> DependentProperties { get; }

        public CodeGenerationOptions Options { get; }

        public RecordDefinition(StructDeclarationSyntax decl, CodeGenerationOptions options)
        {
            Properties = SimpleProperty.New(decl, options).ToArray();
            DependentProperties = DependentProperty.New(decl, Properties, options).ToArray();
            Options = options;
        }
    }

    public class SimpleProperty
    {
        public TypeSyntax Type { get; }
        public string Name { get; }
        public SyntaxTriviaList LeadingTrivia { get; }
        public SyntaxTriviaList TrailingTrivia { get; }
        public CodeGenerationOptions Options { get; }

        public IEnumerable<DependentProperty> Dependents => _dependents;
        private List<DependentProperty> _dependents = new List<DependentProperty>();

        public SimpleProperty(FieldDeclarationSyntax d, CodeGenerationOptions options)
        {
            Type = d.Declaration.Type;
            Name = d.Declaration.Variables[0].Identifier.Text;
            LeadingTrivia = d.GetLeadingTrivia();
            TrailingTrivia = d.GetTrailingTrivia();
            Options = options;
        }

        public static IEnumerable<SimpleProperty> New(StructDeclarationSyntax decl, CodeGenerationOptions options)
            => decl.Members.OfType<FieldDeclarationSyntax>().Select(d => new SimpleProperty(d, options));

        internal void AddDependent(DependentProperty dp) => _dependents.Add(dp);
    }

    public class DependentProperty
    {
        public TypeSyntax Type { get; }
        public string Name { get; }
        public SyntaxTriviaList LeadingTrivia { get; }
        public SyntaxTriviaList TrailingTrivia { get; }
        public IEnumerable<string> DependsOn { get; }
        public CodeGenerationOptions Options { get; }

        public DependentProperty(PropertyDeclarationSyntax d, IEnumerable<SimpleProperty> simpleProperties, CodeGenerationOptions options)
        {
            Type = d.Type;
            Name = d.Identifier.Text;
            LeadingTrivia = d.GetLeadingTrivia();
            TrailingTrivia = d.GetTrailingTrivia();
            DependsOn = GetDependsOn(d, simpleProperties).ToArray();
            Options = options;
        }

        public static IEnumerable<DependentProperty> New(StructDeclarationSyntax decl, IEnumerable<SimpleProperty> simpleProperties, CodeGenerationOptions options)
            => decl.Members.OfType<PropertyDeclarationSyntax>().Select(d => new DependentProperty(d, simpleProperties, options));

        private IEnumerable<string> GetDependsOn(PropertyDeclarationSyntax property, IEnumerable<SimpleProperty> simpleProperties)
        {
            foreach (var p in property
                .DescendantNodes(x => !x.IsKind(SyntaxKind.IdentifierName))
                .OfType<IdentifierNameSyntax>())
            {
                var name = p.Identifier.Text;
                var sp = simpleProperties.FirstOrDefault(x => x.Name == name);

                if (sp != null)
                {
                    sp.AddDependent(this);
                    yield return sp.Name;
                }
            } 
        }
    }
}
