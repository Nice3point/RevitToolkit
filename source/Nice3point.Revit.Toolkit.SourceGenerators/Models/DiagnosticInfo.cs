using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Models;

/// <summary>
///     A serializable model representing diagnostic information,
///     suitable for incremental source generator caching where <see cref="Diagnostic" /> instances cannot be compared by value.
/// </summary>
internal sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    SyntaxTree? SyntaxTree,
    TextSpan TextSpan,
    EquatableArray<string> Arguments)
{
    /// <summary>
    ///     Creates a <see cref="Diagnostic" /> instance from the stored state.
    /// </summary>
    public Diagnostic ToDiagnostic()
    {
        var messageArgs = new object?[Arguments.Length];
        for (var i = 0; i < messageArgs.Length; i++)
        {
            messageArgs[i] = Arguments[i];
        }

        if (SyntaxTree is not null)
        {
            return Diagnostic.Create(Descriptor, Location.Create(SyntaxTree, TextSpan), messageArgs);
        }

        return Diagnostic.Create(Descriptor, null, messageArgs);
    }

    /// <summary>
    ///     Creates a new <see cref="DiagnosticInfo" /> from a <see cref="DiagnosticDescriptor" /> and an <see cref="ISymbol" />.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, ISymbol symbol, params string[] arguments)
    {
        var location = symbol.Locations[0];

        return new DiagnosticInfo(
            descriptor,
            location.SourceTree,
            location.SourceSpan,
            arguments.ToImmutableArray().AsEquatableArray());
    }

    /// <summary>
    ///     Creates a new <see cref="DiagnosticInfo" /> from a <see cref="DiagnosticDescriptor" /> and a <see cref="SyntaxNode" />.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxNode node, params string[] arguments)
    {
        var location = node.GetLocation();

        return new DiagnosticInfo(
            descriptor,
            location.SourceTree,
            location.SourceSpan,
            arguments.ToImmutableArray().AsEquatableArray());
    }
}
