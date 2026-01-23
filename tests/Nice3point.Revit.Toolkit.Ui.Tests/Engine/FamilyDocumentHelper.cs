using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Engine;

/// <summary>
///     Provides helper methods for family document operations in tests.
/// </summary>
internal static class FamilyDocumentHelper
{
    /// <summary>
    ///     Creates a model line in a family document.
    /// </summary>
    public static ModelCurve CreateModelLine(Document document, double length)
    {
        var start = new XYZ(-length / 2, 0, 0);
        var end = new XYZ(length / 2, 0, 0);
        var line = Line.CreateBound(start, end);

        var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
        var sketchPlane = SketchPlane.Create(document, plane);

        return document.FamilyCreate.NewModelCurve(line, sketchPlane);
    }

    /// <summary>
    ///     Validates that the document is a family document and shows error if not.
    /// </summary>
    public static bool RequireFamilyDocument(Document? document)
    {
        if (document is null)
        {
            ShowValidationError("No active document. Please open a document.");
            return false;
        }

        if (!document.IsFamilyDocument)
        {
            ShowValidationError("This test requires a family document. Please open a .rfa file.");
            return false;
        }

        return true;
    }

    private static void ShowValidationError(string message)
    {
        var dialog = new TaskDialog("Test Precondition Failed")
        {
            MainContent = message,
            MainIcon = TaskDialogIcon.TaskDialogIconWarning
        };

        dialog.Show();
    }
}