using System.ComponentModel;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.Helpers;
#if NET
using System.Runtime.Loader;
#endif

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Implementation for a Revit <see cref="Autodesk.Revit.UI.IExternalCommand"/>.
/// </summary>
[PublicAPI]
public abstract class ExternalCommand : IExternalCommand
{
    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.ExternalCommandData" /> that is needed by an external command
    /// </summary>
    public ExternalCommandData ExternalCommandData { get; private set; } = null!;

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

    /// <summary>
    ///     Represents an active session of the Autodesk Revit user interface, providing access to
    ///     UI customization methods, events, the main window, and the active document.
    /// </summary>
    public UIApplication UiApplication => RevitContext.UiApplication;

    /// <summary>
    ///     Represents the database level Autodesk Revit Application, providing access to documents, options and other application wide data and settings.
    /// </summary>
    public Application Application => RevitApiContext.Application;

    /// <summary>Represents a currently active Autodesk Revit project at the UI level</summary>
    /// <remarks>
    ///     External API commands can access this property in read-only mode only.
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">Thrown when attempting to modify the property.</exception>
    public UIDocument ActiveUiDocument => RevitContext.ActiveUiDocument!;

    /// <summary></summary>
    [Obsolete("Use ActiveUiDocument instead")]
    [CodeTemplate(
        searchTemplate: "UIDocument",
        Message = "UIDocument is obsolete, use ActiveUiDocument instead",
        ReplaceTemplate = "ActiveUiDocument",
        ReplaceMessage = "Replace with ActiveUiDocument")]
    public UIDocument UiDocument => RevitContext.ActiveUiDocument!;

    /// <summary>Represents a currently active Autodesk Revit project at the database level</summary>
    /// <remarks>
    ///     Revit can have multiple projects open and multiple views to those projects.
    ///     The active or top most view will be the active project and hence the active document which is available from the Application object.<br/><br/>
    /// </remarks>
    public Document ActiveDocument => RevitContext.ActiveDocument!;

    /// <summary></summary>
    [Obsolete("Use ActiveDocument instead")]
    [CodeTemplate(
        searchTemplate: "Document",
        Message = "Document is obsolete, use ActiveDocument instead",
        ReplaceTemplate = "ActiveDocument",
        ReplaceMessage = "Replace with ActiveDocument")]
    public Document Document => RevitContext.ActiveDocument!;

    /// <summary>Represents the currently active view.</summary>
    public View ActiveView => RevitContext.ActiveView!;

    /// <summary>Callback invoked by Revit. Not used to be called in user code</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        ElementSet = elements;
        ErrorMessage = message;
        ExternalCommandData = commandData;

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