using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ValueChangedGenerator
{
    internal static class RoslynExtensions
    {
        // Code from: https://github.com/YairHalberstadt/stronginject/blob/779a38e7e74b92c87c86ded5d1fef55744d34a83/StrongInject/Generator/RoslynExtensions.cs#L166
        public static string FullName(this INamespaceSymbol @namespace) => @namespace.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

        // Code from: https://github.com/YairHalberstadt/stronginject/blob/779a38e7e74b92c87c86ded5d1fef55744d34a83/StrongInject/Generator/RoslynExtensions.cs#L69
        public static IEnumerable<INamedTypeSymbol> GetContainingTypesAndThis(this INamedTypeSymbol? namedType)
        {
            var current = namedType;
            while (current != null)
            {
                yield return current;
                current = current.ContainingType;
            }
        }
    }
}
