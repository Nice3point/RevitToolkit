using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.SourceGenerators.CSharp;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents.Analysis;

internal sealed class ExternalEventExtensionSignature
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat ConstraintFormat = TypeFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeConstraints);

    private readonly Dictionary<ISymbol, string> _typeParameterNames = new(SymbolEqualityComparer.Default);

    public ExternalEventExtensionDefinition Create(IMethodSymbol method, bool hasUiApplicationParameter, CancellationToken cancellationToken)
    {
        var containingTypes = new Stack<INamedTypeSymbol>();
        for (var containingType = method.ContainingType; containingType is not null; containingType = containingType.ContainingType)
        {
            containingTypes.Push(containingType);
        }

        var reservedParameterNames = new HashSet<string>(method.Parameters.Select(static parameter => parameter.Name), StringComparer.Ordinal)
        {
            "externalEvent"
        };

        var typeParameters = ImmutableArray.CreateBuilder<string>();
        foreach (var containingType in containingTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var parameter in containingType.TypeParameters)
            {
                var typeParameterName = CSharpIdentifier.Escape(CSharpIdentifier.Reserve(parameter.Name, reservedParameterNames));
                _typeParameterNames.Add(parameter, typeParameterName);
                typeParameters.Add(typeParameterName);
            }
        }

        var constraints = new StringBuilder();
        foreach (var containingType in containingTypes)
        {
            var declaration = Format(containingType, ConstraintFormat);
            var constraintIndex = declaration.IndexOf(" where ", StringComparison.Ordinal);
            if (constraintIndex >= 0)
            {
                constraints.Append(declaration[constraintIndex..]);
            }
        }

        return new ExternalEventExtensionDefinition(ExternalEventExtensionContainer.Resolve(method, cancellationToken).ClassAccessibility,
            typeParameters.ToImmutable(), constraints.ToString(),
            $"{Format(method.ContainingType, TypeFormat)}.{method.Name}Args",
            method.ReturnsVoid ? null : Format(method.ReturnType, TypeFormat),
            method.Parameters
                .Skip(hasUiApplicationParameter ? 1 : 0)
                .Select(parameter => Format(parameter.Type, TypeFormat))
                .ToImmutableArray());
    }

    private string Format(ISymbol symbol, SymbolDisplayFormat format)
    {
        var text = new StringBuilder();
        foreach (var part in symbol.ToDisplayParts(format))
        {
            text.Append(part.Symbol is ITypeParameterSymbol parameter && _typeParameterNames.TryGetValue(parameter, out var typeParameterName)
                ? typeParameterName
                : part.ToString());
        }

        return text.ToString();
    }
}
