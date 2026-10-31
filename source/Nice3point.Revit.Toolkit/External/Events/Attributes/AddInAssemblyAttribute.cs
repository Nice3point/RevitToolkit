using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Specifies an assembly that runs its external events as the add-in of the declaring assembly.
/// </summary>
/// <remarks>
///     The Toolkit source generator declares the attribute for each project reference of the add-in.
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
