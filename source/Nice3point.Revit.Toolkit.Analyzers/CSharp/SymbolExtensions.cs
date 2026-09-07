using Microsoft.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.Analyzers.CSharp;

internal static class SymbolExtensions
{
    extension(ISymbol symbol)
    {
        public bool HasAttribute(INamedTypeSymbol attributeSymbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
