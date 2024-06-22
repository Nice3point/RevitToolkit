using System.ComponentModel;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
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
    private bool _suppressFailures;

    private SuppressException _suppressException;
    private Action<Exception> _exceptionHandler;

    private int _dialogResultCode;
    private SuppressDialog _suppressDialog;
    private Action<DialogBoxShowingEventArgs> _dialogBoxHandler;

    /// <summary>
    ///     Element set indicating problem elements to display in the failure dialog. This will be used only if the command status was "Failed".
    /// </summary>
    public ElementSet ElementSet { get; private set; }

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.ExternalCommandData" /> that is needed by an external command
    /// </summary>
    public ExternalCommandData ExternalCommandData { get; private set; }

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
    public View ActiveView => Context.ActiveView;

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
            return AddinLoadContext.ExecuteCommand(instance, commandData, ref message, elements);
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
        catch (Exception exception)
        {
            switch (_suppressException)
            {
                case SuppressException.Full:
                    message = ErrorMessage;
                    return Result.Failed;
                case SuppressException.Handler:
                    _exceptionHandler?.Invoke(exception);
                    message = ErrorMessage;
                    return Result.Failed;
            }

            throw;
        }
        finally
        {
            message = ErrorMessage;
            RestoreFailures();
            RestoreDialogs();
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
    /// <remarks>Does not affect the output of the ErrorMessage</remarks>
    public void SuppressExceptions()
    {
        _suppressException = SuppressException.Full;
    }

    /// <summary>
    ///     Suppresses exceptions in external command
    /// </summary>
    /// <remarks>Does not affect the output of the ErrorMessage</remarks>
    public void SuppressExceptions(Action<Exception> handler)
    {
        _suppressException = SuppressException.Handler;
        _exceptionHandler = handler;
    }

    /// <summary>
    ///     Suppresses failures in external command
    /// </summary>
    /// <remarks>Deletes all FailureMessages of severity "Warning"</remarks>
    public void SuppressFailures()
    {
        if (_suppressFailures) return;

        _suppressFailures = true;
        Application.FailuresProcessing += ResolveFailures;
    }

    /// <summary>
    ///     Suppresses the display of dialog box or a message box
    /// </summary>
    /// <param name="result">The result code you wish the Revit dialog to return</param>
    /// <remarks>
    ///     The range of valid result values depends on the type of dialog as follows:
    ///     <list type="number">
    ///         <item>
    ///             DialogBox: Any non-zero value will cause a dialog to be dismissed.
    ///         </item>
    ///         <item>
    ///             MessageBox: Standard Message Box IDs, such as IDOK and IDCANCEL, are accepted.
    ///             For all possible IDs, refer to the Windows API documentation.
    ///             The ID used must be relevant to the buttons in a message box.
    ///         </item>
    ///         <item>
    ///             TaskDialog: Standard Message Box IDs and Revit Custom IDs are accepted,
    ///             depending on the buttons used in a dialog. Standard buttons, such as OK
    ///             and Cancel, have standard IDs described in Windows API documentation.
    ///             Buttons with custom text have custom IDs with incremental values
    ///             starting at 1001 for the left-most or top-most button in a task dialog.
    ///         </item>
    ///     </list>
    /// </remarks>
    public void SuppressDialogs(int result = 1)
    {
        if (_suppressDialog == SuppressDialog.None) UiApplication.DialogBoxShowing += ResolveDialogBox;
        _suppressDialog = SuppressDialog.ResultCode;
        _dialogResultCode = result;
    }

    /// <summary>
    ///     Suppresses the display of dialog box or a message box
    /// </summary>
    /// <param name="handler">Dialog handler</param>
    public void SuppressDialogs(Action<DialogBoxShowingEventArgs> handler)
    {
        if (_suppressDialog == SuppressDialog.None) UiApplication.DialogBoxShowing += ResolveDialogBox;
        _suppressDialog = SuppressDialog.Handler;
        _dialogBoxHandler = handler;
    }

    /// <summary>
    ///     Restores display of dialog box or a message box
    /// </summary>
    public void RestoreDialogs()
    {
        if (_suppressDialog != SuppressDialog.None) UiApplication.DialogBoxShowing -= ResolveDialogBox;
        _suppressDialog = SuppressDialog.None;
    }

    /// <summary>
    ///     Restores failure handling
    /// </summary>
    public void RestoreFailures()
    {
        if (_suppressFailures == false) return;

        Application.FailuresProcessing -= ResolveFailures;
        _suppressFailures = false;
    }

    private void ResolveDialogBox(object sender, DialogBoxShowingEventArgs args)
    {
        switch (_suppressDialog)
        {
            case SuppressDialog.ResultCode:
                args.OverrideResult(_dialogResultCode);
                break;
            case SuppressDialog.Handler:
                _dialogBoxHandler?.Invoke(args);
                break;
        }
    }

    private void ResolveFailures(object sender, FailuresProcessingEventArgs args)
    {
        args.GetFailuresAccessor().DeleteAllWarnings();
    }
}

[PublicAPI]
internal enum SuppressDialog
{
    None,
    ResultCode,
    Handler
}

[PublicAPI]
internal enum SuppressException
{
    None,
    Full,
    Handler
}