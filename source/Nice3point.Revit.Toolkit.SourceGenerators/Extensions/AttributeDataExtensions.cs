using Microsoft.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Extensions;

/// <summary>
///     Extension methods for the <see cref="AttributeData"/> type.
/// </summary>
internal static class AttributeDataExtensions
{
    /// <summary>
    ///     Gets a given named argument value from an <see cref="AttributeData"/> instance, or a fallback value.
    /// </summary>
    public static T? GetNamedArgument<T>(this AttributeData attributeData, string name, T? fallback = default)
    {
        if (attributeData.TryGetNamedArgument(name, out T? value))
        {
            return value;
        }

        return fallback;
    }

    /// <summary>
    ///     Tries to get a given named argument value from an <see cref="AttributeData"/> instance, if present.
    /// </summary>
    public static bool TryGetNamedArgument<T>(this AttributeData attributeData, string name, out T? value)
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

    /// <summary>
    ///     Checks whether a given <see cref="AttributeData"/> instance contains a specified named argument.
    /// </summary>
    public static bool HasNamedArgument<T>(this AttributeData attributeData, string name, T? value)
    {
        foreach (var properties in attributeData.NamedArguments)
        {
            if (properties.Key == name)
            {
                return properties.Value.Value is T argumentValue &&
                       EqualityComparer<T?>.Default.Equals(argumentValue, value);
            }
        }

        return false;
    }
}