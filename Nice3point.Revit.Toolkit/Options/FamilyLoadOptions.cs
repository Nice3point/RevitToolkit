using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.Options;

/// <summary>
///     A class which provide the callback for family load options.
/// </summary>
/// <example>
///     <code>
///         document.LoadFamily(fileName, new FamilyLoadOptions(), out var family);
///     </code>
/// </example>
public class FamilyLoadOptions : IFamilyLoadOptions
{
    /// <summary>A method called when the family was found in the target document</summary>
    /// <remarks>Triggered only when the family is both loaded and changed</remarks>
    /// <param name="familyInUse">
    ///     Indicates if one or more instances of the family is placed in the project
    /// </param>
    /// <param name="overwriteParameterValues">
    ///     This determines whether or not to overwrite the parameter values of existing types. The default value is false
    /// </param>
    /// <returns>Return true to continue loading the family, false to cancel</returns>
    public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
        overwriteParameterValues = false;
        if (familyInUse)
        {
            var taskDialog = new TaskDialog("Family already exists")
            {
                TitleAutoPrefix = false,
                MainInstruction = "You are trying to load the family which already exists in this project. What do you want to do?",
                CommonButtons = TaskDialogCommonButtons.Cancel,
                DefaultButton = TaskDialogResult.Cancel
            };

            taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Overwrite the existing version");
            taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Overwrite the existing version and its parameter values");
            var result = taskDialog.Show();
            switch (result)
            {
                case TaskDialogResult.Cancel:
                    return false;
                case TaskDialogResult.CommandLink2:
                    overwriteParameterValues = true;
                    break;
            }
        }

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
    public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
    {
        source = FamilySource.Family;
        overwriteParameterValues = false;
        return true;
    }
}