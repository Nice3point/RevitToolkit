using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Implementation for a Revit add-in External Command
/// </summary>
public abstract class ExternalCommand : IExternalCommand
{
    private Action<DialogBoxShowingEventArgs> _dialogBoxHandler;
    private int _dialogBoxResult;
    private Action<Exception> _exceptionHandler;
    private SuppressDialog _suppressDialog;
    private SuppressException _suppressException;

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
    public UIApplication UiApplication { get; set; }

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.ApplicationServices.Application" /> that is needed by an external command
    /// </summary>
    public Application Application => UiApplication.Application;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.UIDocument" /> that is needed by an external command
    /// </summary>
    public UIDocument UiDocument => UiApplication.ActiveUIDocument;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.DB.Document" /> that is needed by an external command
    /// </summary>
    public Document Document => UiApplication.ActiveUIDocument.Document;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.UIDocument.ActiveView" /> that is needed by an external command
    /// </summary>
    public View ActiveView => UiApplication.ActiveUIDocument.ActiveView;

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

    /// <summary>Override this method to implement and external command within Revit.</summary>
    /// <returns>
    ///     The result indicates if the execution fails, succeeds, or was canceled by user. If it does not
    ///     succeed, Revit will undo any changes made by the external command.
    /// </returns>
    /// <param name="commandData">
    ///     An ExternalCommandData object which contains reference to Application and View
    ///     needed by external command.
    /// </param>
    /// <param name="message">
    ///     Error message can be returned by external command. This will be displayed only if the command status
    ///     was "Failed".  There is a limit of 1023 characters for this message; strings longer than this will be truncated.
    /// </param>
    /// <param name="elements">
    ///     Element set indicating problem elements to display in the failure dialog.  This will be used
    ///     only if the command status was "Failed".
    /// </param>
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        ElementSet = elements;
        ErrorMessage = message;
        ExternalCommandData = commandData;
        UiApplication = commandData.Application;
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        try
        {
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
            RestoreDialogs();
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
            message = ErrorMessage;
        }

        return Result;
    }

    /// <summary>
    ///     Overload this method to implement and external command within Revit
    /// </summary>
    public virtual void Execute()
    {
    }

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
        _suppressDialog = SuppressDialog.Code;
        _dialogBoxResult = result;
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

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var assemblies = Directory.EnumerateFiles(assemblyDirectory, "*.dll");
        foreach (var assembly in assemblies)
            if (assemblyName == Path.GetFileNameWithoutExtension(assembly))
                return Assembly.LoadFile(assembly);

        return null;
    }

    private void ResolveDialogBox(object sender, DialogBoxShowingEventArgs e)
    {
        switch (_suppressDialog)
        {
            case SuppressDialog.Code:
                e.OverrideResult(_dialogBoxResult);
                break;
            case SuppressDialog.Handler:
                _dialogBoxHandler?.Invoke(e);
                break;
        }
    }
}

internal enum SuppressDialog
{
    None,
    Code,
    Handler
}

internal enum SuppressException
{
    None,
    Full,
    Handler
}