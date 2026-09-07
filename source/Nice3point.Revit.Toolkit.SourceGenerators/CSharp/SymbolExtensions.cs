// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.SourceGenerators.CSharp;

/// <summary>
///     Extension methods for the <see cref="ISymbol" /> type.
/// </summary>
internal static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat NullableTypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <param name="symbol">The input <see cref="ISymbol" /> instance.</param>
    extension(ISymbol symbol)
    {
        /// <summary>
        ///     Gets the fully qualified name for a given symbol, including nullability annotations
        /// </summary>
        /// <returns>The fully qualified name for <paramref name="symbol" />.</returns>
        public string GetFullyQualifiedNameWithNullabilityAnnotations()
        {
            return symbol.ToDisplayString(NullableTypeFormat);
        }
    }
}
