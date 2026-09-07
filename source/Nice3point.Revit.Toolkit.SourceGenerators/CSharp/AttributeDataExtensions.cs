using Microsoft.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.SourceGenerators.CSharp;

/// <summary>
///     Extension methods for the <see cref="AttributeData" /> type.
/// </summary>
internal static class AttributeDataExtensions
{
    extension(AttributeData attributeData)
    {
        /// <summary>
        ///     Gets a given named argument value from an <see cref="AttributeData" /> instance, or a fallback value.
        /// </summary>
        public T? GetNamedArgument<T>(string name, T? fallback = default)
        {
            if (attributeData.TryGetNamedArgument(name, out T? value))
            {
                return value;
            }

            return fallback;
        }

        /// <summary>
        ///     Tries to get a given named argument value from an <see cref="AttributeData" /> instance, if present.
        /// </summary>
        public bool TryGetNamedArgument<T>(string name, out T? value)
        {
            foreach (var properties in attributeData.NamedArguments)
            {
                if (properties.Key == name)
                {
                    value = (T?)properties.Value.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
