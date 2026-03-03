namespace Nice3point.Revit.Toolkit.SourceGenerators.Models;

/// <summary>
///     Represents a fully qualified type name with both <c>global::</c>-prefixed
///     and unprefixed forms for use in source generation.
/// </summary>
internal sealed class FullyQualifiedTypeName
{
    /// <summary>
    ///     Initializes a new instance from a fully qualified type name without the <c>global::</c> prefix.
    /// </summary>
    public FullyQualifiedTypeName(string fullyQualifiedType)
    {
        WithoutGlobalPrefix = fullyQualifiedType;
        WithGlobalPrefix = $"global::{WithoutGlobalPrefix}";
    }

    /// <summary>
    ///     Gets the fully qualified type name without the <c>global::</c> prefix.
    /// </summary>
    public string WithoutGlobalPrefix { get; }

    /// <summary>
    ///     Gets the fully qualified type name with the <c>global::</c> prefix.
    /// </summary>
    public string WithGlobalPrefix { get; }

    /// <summary>
    ///     Implicitly converts a string to a <see cref="FullyQualifiedTypeName"/>.
    /// </summary>
    public static implicit operator FullyQualifiedTypeName(string name)
    {
        return new FullyQualifiedTypeName(name);
    }

    public override string ToString()
    {
        return WithGlobalPrefix;
    }
}