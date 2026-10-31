using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Specifies an assembly whose external events run as the add-in of the assembly that declares the attribute.
/// </summary>
/// <remarks>
///     The toolkit source generator declares the attribute for every project reference of a project that references the toolkit package.
///     The assembly is matched by name at its first external event created outside a Revit callback.
/// </remarks>
[PublicAPI]
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class AddInAssemblyAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AddInAssemblyAttribute" /> class.
    /// </summary>
    /// <param name="assemblyName">The simple name of the assembly.</param>
    public AddInAssemblyAttribute(string assemblyName)
    {
        AssemblyName = assemblyName;
    }

    /// <summary>
    ///     Gets the simple name of the assembly.
    /// </summary>
    public string AssemblyName { get; }
}
