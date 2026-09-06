using System.ComponentModel;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.Helpers;
#if NET
using System.Runtime.Loader;
#endif

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Implementation for a Revit <see cref="Autodesk.Revit.UI.IExternalCommand" />.
/// </summary>
[PublicAPI]
public abstract class ExternalCommand : IExternalCommand
{
    /// <summary>
    ///     An object that represents the current Application for external command.
    /// </summary>
    public UIApplication Application { get; private set; } = null!;

    /// <summary>
    ///     An object that represents the View external command work on.
    /// </summary>
    public View View { get; private set; } = null!;

    /// <summary>
    ///     A data map that can be used to read and write data to the Autodesk Revit journal file.
    /// </summary>
    /// <remarks>
    ///     The data map is a string to string map that can be used to store data in the Revit journal
    ///     file at the end of execution of the external command. If the command is then executed from the journal
    ///     file during playback this data is then passed to the external command in this Data property so the
    ///     external command can execute with this passed data in a UI-less mode, hence providing non interactive
    ///     journal playback for automated testing purposes. For more information on Revit's journaling features
    ///     contact the Autodesk Developer Network.
    /// </remarks>
    public IDictionary<string, string> JournalData { get; private set; } = null!;

    /// <summary>
    ///     Error message can be returned by external command. This will be displayed only if the command status was "Failed" <br />
    ///     There is a limit of 1023 characters for this message; strings longer than this will be truncated.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    ///     Element set indicating problem elements to display in the failure dialog. This will be used only if the command status was "Failed".
    /// </summary>
    public ElementSet ElementSet { get; private set; } = null!;

    /// <summary>
    ///     Informs Autodesk Revit of the status of your application after execution.
    /// </summary>
    /// <remarks>
    ///     The result indicates if the execution fails, succeeds, or was canceled by user. If it does not
    ///     succeed, Revit will undo any changes made by the external command
    /// </remarks>
    public Result Result { get; set; } = Result.Succeeded;

    /// <summary></summary>
    [Obsolete("Use Application instead")]
    [CodeTemplate(
        "UiApplication",
        Message = "UiApplication is obsolete, use Application instead",
        ReplaceTemplate = "Application",
        ReplaceMessage = "Replace with Application")]
    public UIApplication UiApplication => Application;

    /// <summary></summary>
    [Obsolete("Use Application.ActiveUIDocument instead")]
    [CodeTemplate(
        "ActiveUiDocument",
        Message = "ActiveUiDocument is obsolete, use Application.ActiveUIDocument instead",
        ReplaceTemplate = "Application.ActiveUIDocument",
        ReplaceMessage = "Replace with Application.ActiveUIDocument")]
    public UIDocument ActiveUiDocument => Application.ActiveUIDocument;

    /// <summary></summary>
    [Obsolete("Use Application.ActiveUIDocument instead")]
    [CodeTemplate(
        "UiDocument",
        Message = "UiDocument is obsolete, use Application.ActiveUIDocument instead",
        ReplaceTemplate = "Application.ActiveUIDocument",
        ReplaceMessage = "Replace with Application.ActiveUIDocument")]
    public UIDocument UiDocument => Application.ActiveUIDocument;

    /// <summary></summary>
    [Obsolete("Use Application.ActiveUIDocument.Document instead")]
    [CodeTemplate(
        "ActiveDocument",
        Message = "ActiveDocument is obsolete, use Application.ActiveUIDocument.Document instead",
        ReplaceTemplate = "Application.ActiveUIDocument.Document",
        ReplaceMessage = "Replace with Application.ActiveUIDocument.Document")]
    public Document ActiveDocument => Application.ActiveUIDocument.Document;

    /// <summary></summary>
    [Obsolete("Use Application.ActiveUIDocument.Document instead")]
    [CodeTemplate(
        "Document",
        Message = "Document is obsolete, use Application.ActiveUIDocument.Document instead",
        ReplaceTemplate = "Application.ActiveUIDocument.Document",
        ReplaceMessage = "Replace with Application.ActiveUIDocument.Document")]
    public Document Document => Application.ActiveUIDocument.Document;

    /// <summary></summary>
    [Obsolete("Use Application.ActiveUIDocument.ActiveView instead")]
    [CodeTemplate(
        "ActiveView",
        Message = "ActiveView is obsolete, use Application.ActiveUIDocument.ActiveView instead",
        ReplaceTemplate = "Application.ActiveUIDocument.ActiveView",
        ReplaceMessage = "Replace with Application.ActiveUIDocument.ActiveView")]
    public View ActiveView => Application.ActiveUIDocument.ActiveView;

    /// <summary></summary>
    [Obsolete("Use Application, View, or JournalData instead")]
    public ExternalCommandData ExternalCommandData { get; private set; } = null!;

    /// <summary>Callback invoked by Revit. Not used to be called in user code</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        ElementSet = elements;
        ErrorMessage = message;
#pragma warning disable CS0618 // Type or member is obsolete
        ExternalCommandData = commandData;
#pragma warning restore CS0618 // Type or member is obsolete
        Application = commandData.Application;
        View = commandData.View;
        JournalData = commandData.JournalData;

        var currentType = GetType();
#if NET
        if (AssemblyLoadContext.GetLoadContext(currentType.Assembly) == AssemblyLoadContext.Default)
        {
            using (ResolveHelper.BeginAssemblyResolveScope(currentType))
            {
                Execute();
            }
        }
        else
        {
            Execute();
        }
#else
        using (ResolveHelper.BeginAssemblyResolveScope(currentType))
        {
            Execute();
        }
#endif

        message = ErrorMessage;
        return Result;
    }

    /// <summary>
    ///     Overload this method to implement and external command within Revit
    /// </summary>
    public abstract void Execute();
}
