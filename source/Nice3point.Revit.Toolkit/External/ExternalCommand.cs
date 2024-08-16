using System.ComponentModel;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Nice3point.Revit.Toolkit.Helpers;

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Implementation for a Revit add-in External Command
/// </summary>
[PublicAPI]
public abstract class ExternalCommand : IExternalCommand
{
    private bool _suppressExceptions;

    /// <summary>
    ///     Element set indicating problem elements to display in the failure dialog. This will be used only if the command status was "Failed".
    /// </summary>
    public ElementSet ElementSet { get; private set; } = default!;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.ExternalCommandData" /> that is needed by an external command
    /// </summary>
    public ExternalCommandData ExternalCommandData { get; private set; } = default!;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.UIApplication" /> that is needed by an external command
    /// </summary>
    public UIApplication UiApplication => Context.UiApplication;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.ApplicationServices.Application" /> that is needed by an external command
    /// </summary>
    public Application Application => Context.Application;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.UIDocument" /> that is needed by an external command
    /// </summary>
    public UIDocument UiDocument => Context.UiDocument;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.DB.Document" /> that is needed by an external command
    /// </summary>
    public Document Document => Context.Document;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.UIDocument.ActiveView" /> that is needed by an external command
    /// </summary>
    public View ActiveView => Context.ActiveView!;

    /// <summary>
    ///     Informs Autodesk Revit of the status of your application after execution.
    /// </summary>
    /// <remarks>
    ///     The result indicates if the execution fails, succeeds, or was canceled by user. If it does not
    ///     succeed, Revit will undo any changes made by the external command
    /// </remarks>
    public Result Result { get; set; } = Result.Succeeded;

    /// <summary>
    ///     Error message can be returned by external command. This will be displayed only if the command status was "Failed" <br />
    ///     There is a limit of 1023 characters for this message; strings longer than this will be truncated.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Callback invoked by Revit. Not used to be called in user code</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var currentType = GetType();

#if NETCOREAPP
        if (!AddinLoadContext.CheckAccess(currentType))
        {
            var dependenciesProvider = AddinLoadContext.GetDependenciesProvider(currentType);
            var instance = dependenciesProvider.CreateInstance(currentType);
            return AddinLoadContext.Invoke(instance, commandData, ref message, elements);
        }
#endif

        ElementSet = elements;
        ErrorMessage = message;
        ExternalCommandData = commandData;

        try
        {
#if !NETCOREAPP
            ResolveHelper.BeginAssemblyResolve(currentType);
#endif
            Execute();
        }
        catch
        {
            if (_suppressExceptions)
            {
                //We must cancel the command because the Failed result shows Error dialog
                return Result.Cancelled;
            }

            Result = Result.Failed;
            throw;
        }
        finally
        {
            message = ErrorMessage;
            Context.RestoreFailures();
            Context.RestoreDialogs();
#if !NETCOREAPP
            ResolveHelper.EndAssemblyResolve();
#endif
        }

        return Result;
    }

    /// <summary>
    ///     Overload this method to implement and external command within Revit
    /// </summary>
    public abstract void Execute();

    /// <summary>
    ///     Suppresses exceptions in external command
    /// </summary>
    /// <remarks>
    ///     Removes unhandled exception messages that include stackTrace<br />
    ///     Shows Error dialog if an ErrorMessage is provided
    /// </remarks>
    [Obsolete("SuppressExceptions will be removed in the next Major version")]
    public void SuppressExceptions()
    {
        _suppressExceptions = true;
    }

    /// <summary>
    ///     Suppresses failures in external command
    /// </summary>
    /// <remarks>Deletes all FailureMessages of severity "Warning"</remarks>
    [Obsolete("SuppressFailures will be removed in the next Major version, use Context.SuppressFailures() instead")]
    public void SuppressFailures() => Context.SuppressFailures();

    /// <summary>
    ///     Suppresses the display of dialog box or a message box
    /// </summary>
    /// <param name="result">The result code you wish the Revit dialog to return</param>
    [Obsolete("SuppressExceptions will be removed in the next Major version, use Context.SuppressDialogs() instead")]
    public void SuppressDialogs(int result = 1) => Context.SuppressDialogs(result);

    /// <summary>
    ///     Suppresses the display of dialog box or a message box
    /// </summary>
    /// <param name="handler">Dialog handler</param>
    [Obsolete("SuppressExceptions will be removed in the next Major version, use Context.SuppressDialogs() instead")]
    public void SuppressDialogs(Action<DialogBoxShowingEventArgs> handler) => Context.SuppressDialogs(handler);

    /// <summary>
    ///     Restores display of dialog box or a message box
    /// </summary>
    [Obsolete("SuppressExceptions will be removed in the next Major version, use Context.RestoreDialogs() instead")]
    public void RestoreDialogs() => Context.RestoreDialogs();

    /// <summary>
    ///     Restores failure handling
    /// </summary>
    [Obsolete("SuppressExceptions will be removed in the next Major version, use Context.RestoreFailures() instead")]
    public void RestoreFailures() => Context.RestoreFailures();
}