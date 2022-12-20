using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ValueChangedGenerator
{
    public class RecordDefinition
    {
        public IReadOnlyList<SimpleProperty> Properties { get; }

        public IReadOnlyList<DependentProperty> DependentProperties { get; }

        public RecordDefinition(StructDeclarationSyntax decl)
        {
            Properties = SimpleProperty.New(decl).ToArray();
            DependentProperties = DependentProperty.New(decl, Properties).ToArray();
        }
    }

    public class SimpleProperty
    {
        public TypeSyntax Type { get; }
        public string Name { get; }
        public SyntaxTriviaList LeadingTrivia { get; }
        public SyntaxTriviaList TrailingTrivia { get; }

        public IEnumerable<DependentProperty> Dependents => _dependents;
        private readonly List<DependentProperty> _dependents = new List<DependentProperty>();

        public SimpleProperty(FieldDeclarationSyntax d)
        {
            Type = d.Declaration.Type;
            Name = d.Declaration.Variables[0].Identifier.Text;
            LeadingTrivia = d.GetLeadingTrivia();
            TrailingTrivia = d.GetTrailingTrivia();
        }

        public static IEnumerable<SimpleProperty> New(StructDeclarationSyntax decl)
            => decl.Members.OfType<FieldDeclarationSyntax>().Select(d => new SimpleProperty(d));

        internal void AddDependent(DependentProperty dp) => _dependents.Add(dp);
    }

    public class DependentProperty
    {
        public TypeSyntax Type { get; }
        public string Name { get; }
        public SyntaxTriviaList LeadingTrivia { get; }
        public SyntaxTriviaList TrailingTrivia { get; }
        public IEnumerable<string> DependsOn { get; }

        public DependentProperty(PropertyDeclarationSyntax d, IEnumerable<SimpleProperty> simpleProperties)
        {
            Type = d.Type;
            Name = d.Identifier.Text;
            LeadingTrivia = d.GetLeadingTrivia();
            TrailingTrivia = d.GetTrailingTrivia();
            DependsOn = GetDependsOn(d, simpleProperties).ToArray();
        }

        public static IEnumerable<DependentProperty> New(StructDeclarationSyntax decl, IEnumerable<SimpleProperty> simpleProperties)
            => decl.Members.OfType<PropertyDeclarationSyntax>().Select(d => new DependentProperty(d, simpleProperties));

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
