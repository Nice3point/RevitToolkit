// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Extensions;

/// <summary>
///     Extension methods for the <see cref="ISymbol" /> type.
/// </summary>
[PublicAPI]
internal static class SymbolExtensions
{
    /// <param name="symbol">The input <see cref="ISymbol" /> instance.</param>
    extension(ISymbol symbol)
    {
        /// <summary>
        ///     Gets the fully qualified name for a given symbol.
        /// </summary>
        /// <returns>The fully qualified name for <paramref name="symbol" />.</returns>
        public string GetFullyQualifiedName()
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        /// <summary>
        ///     Gets the fully qualified name for a given symbol, including nullability annotations
        /// </summary>
        /// <returns>The fully qualified name for <paramref name="symbol" />.</returns>
        public string GetFullyQualifiedNameWithNullabilityAnnotations()
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
        }

        /// <summary>
        ///     Checks whether or not a given type symbol has a specified full name.
        /// </summary>
        /// <param name="name">The full name to check.</param>
        /// <returns>Whether <paramref name="symbol" /> has a full name equals to <paramref name="name" />.</returns>
        public bool HasFullyQualifiedName(string name)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == name;
        }
    }
}
