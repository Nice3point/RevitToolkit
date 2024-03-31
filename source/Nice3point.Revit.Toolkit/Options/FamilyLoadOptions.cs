using System.ComponentModel;

namespace Nice3point.Revit.Toolkit.Options;

/// <summary>
///     A class which provide the callback for family load options.
/// </summary>
/// <example>
///     <code>
///         document.LoadFamily(fileName, new FamilyLoadOptions(), out var family);
///         document.LoadFamily(fileName, UIDocument.GetRevitUIFamilyLoadOptions(), out var family);
///     </code>
/// </example>
[PublicAPI]
public class FamilyLoadOptions : IFamilyLoadOptions
{
    private readonly FamilySource _familySource;
    private readonly bool _overwrite;

    /// <summary>
    ///     Return the option object that allows families to be loaded
    /// </summary>
    /// <remarks>Overwrites parameter values of existing types</remarks>
    public FamilyLoadOptions()
    {
        _overwrite = true;
        _familySource = FamilySource.Family;
    }

    /// <summary>
    ///     Return the option object that allows families to be loaded
    /// </summary>
    /// <param name="overwrite">This determines whether or not to overwrite the parameter values of existing types</param>
    public FamilyLoadOptions(bool overwrite)
    {
        _overwrite = overwrite;
        _familySource = FamilySource.Family;
    }

    /// <summary>
    ///     Return the option object that allows families to be loaded
    /// </summary>
    /// <param name="overwrite">This determines whether or not to overwrite the parameter values of existing types</param>
    /// <param name="familySource">This indicates if the family will load from the project or the current family</param>
    public FamilyLoadOptions(bool overwrite, FamilySource familySource)
    {
        _overwrite = overwrite;
        _familySource = familySource;
    }

    /// <summary>A method called when the family was found in the target document</summary>
    /// <remarks>Triggered only when the family is both loaded and changed</remarks>
    /// <param name="familyInUse">
    ///     Indicates if one or more instances of the family is placed in the project
    /// </param>
    /// <param name="overwriteParameterValues">
    ///     This determines whether or not to overwrite the parameter values of existing types. The default value is false
    /// </param>
    /// <returns>Return true to continue loading the family, false to cancel</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
        overwriteParameterValues = _overwrite;
        return true;
    }

    /// <summary>A method called when the shared family was found in the target document</summary>
    /// <remarks>Triggered only when the family is both loaded and changed</remarks>
    /// <param name="sharedFamily">The shared family in the current family document</param>
    /// <param name="familyInUse">
    ///     Indicates if one or more instances of the family is placed in the project
    /// </param>
    /// <param name="source">
    ///     This indicates if the family will load from the project or the current family
    /// </param>
    /// <param name="overwriteParameterValues">
    ///     This indicates whether or not to overwrite the parameter values of existing types
    /// </param>
    /// <returns>Return true to continue loading the family, false to cancel</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
    {
        overwriteParameterValues = _overwrite;
        source = _familySource;
        return true;
    }
}