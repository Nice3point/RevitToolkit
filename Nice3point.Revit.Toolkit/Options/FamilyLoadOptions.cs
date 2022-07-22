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
    public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
        overwriteParameterValues = false;
        if (familyInUse)
        {
            var taskDialog = new TaskDialog("Family already exists");
            taskDialog.TitleAutoPrefix = false;
            taskDialog.MainInstruction = "You are trying to load the family which already exists in this project. What do you want to do?";
            taskDialog.CommonButtons = TaskDialogCommonButtons.Cancel;
            taskDialog.DefaultButton = TaskDialogResult.Cancel;
            taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Overwrite the existing version");
            taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Overwrite the existing version and its parameter values");
            var result = taskDialog.Show();
            switch (result)
            {
                case TaskDialogResult.Cancel:
                    return false;
                case TaskDialogResult.CommandLink2:
                    overwriteParameterValues = false;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        return true;
    }

    public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
    {
        source = FamilySource.Family;
        overwriteParameterValues = false;
        return true;
    }
}