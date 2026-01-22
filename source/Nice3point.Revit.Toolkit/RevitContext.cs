#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using System.Reflection;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using JetBrains.Annotations;

namespace Nice3point.Revit.Toolkit;

/// <summary>
///     Provides members for accessing the Revit application context at the UI level.
/// </summary>
[PublicAPI]
public class RevitContext : RevitApiContext
{
    //Global state
    private static readonly Lock DialogLock = new();
    private static bool _suppressDialogs;
    private static int? _suppressDialogCode;
    private static Action<DialogBoxShowingEventArgs>? _suppressDialogHandler;
    private static readonly Func<bool> GetIsRevitInApiMode;

    static RevitContext()
    {
        var apiUiAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "APIUIAPI");
        ThrowWhen(apiUiAssembly is null);

        var apiAssemblyMethods = apiUiAssembly.ManifestModule.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
        var apiCallDepthManagerMethod = apiAssemblyMethods.FirstOrDefault(info => info.Name == "APICallDepthManager.singletonfactory");
        ThrowWhen(apiCallDepthManagerMethod is null);

        var isRevitInApiModeMethod = apiAssemblyMethods.FirstOrDefault(info => info.Name == "APICallDepthManager.isRevitInAPIMode");
        ThrowWhen(isRevitInApiModeMethod is null);

        GetIsRevitInApiMode = () =>
        {
            var apiCallDepthManager = apiCallDepthManagerMethod.Invoke(null, null);
            return (bool)isRevitInApiModeMethod.Invoke(null, [apiCallDepthManager])!;
        };

        UiApplication = new UIApplication(Application);
    }

    /// <summary>
    ///     Represents an active session of the Autodesk Revit user interface, providing access to
    ///     UI customization methods, events, the main window, and the active document.
    /// </summary>
    public static UIApplication UiApplication { get; }

    /// <summary>
    ///     Represents the Autodesk Revit user interface, providing access to UI customization methods and events.
    /// </summary>
    public static UIControlledApplication UiControlledApplication =>
#if NET8_0_OR_GREATER
        CreateUiControlledApplication(UiApplication);
#else
        (UIControlledApplication)Activator.CreateInstance(
            typeof(UIControlledApplication),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [UiApplication],
            null)!;
#endif

    /// <summary>Represents a currently active Autodesk Revit project at the UI level.</summary>
    /// <remarks>
    ///     External API commands can access this property in read-only mode only.
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">Thrown when attempting to modify the property.</exception>
    /// <returns>
    ///     Currently active project.<br/>
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </returns>
    public static UIDocument? ActiveUiDocument => UiApplication.ActiveUIDocument;

    /// <summary>Represents a currently active Autodesk Revit project at the database level.</summary>
    /// <remarks>
    ///     Revit can have multiple projects open and multiple views to those projects.
    ///     The active or top most view will be the active project and hence the active document which is available from the Application object.<br/><br/>
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </remarks>
    public static Document? ActiveDocument => UiApplication.ActiveUIDocument?.Document;

    /// <summary>Represents the currently active view of the currently active document.</summary>
    /// <remarks>
    ///     <para>
    ///         This property is applicable to the currently active document only.<br/>
    ///         Returns <see langword="null" /> if there are no active projects.
    ///     </para>
    ///     <para>
    ///         The active view can only be changed when:
    ///         <ul>
    ///             <li>There is no open transaction.</li><li><see cref="P:Autodesk.Revit.DB.Document.IsModifiable" /> is false.</li>
    ///             <li><see cref="P:Autodesk.Revit.DB.Document.IsReadOnly" /> is false.</li>
    ///             <li>ViewActivating, ViewActivated, and any pre-action of events (such as DocumentSaving or DocumentClosing events) are not being handled.</li>
    ///         </ul>
    ///     </para>
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.ArgumentNullException">
    ///     When setting the property: If the 'view' argument is NULL.
    /// </exception>
    /// <exception cref="T:Autodesk.Revit.Exceptions.ArgumentException">
    ///     When setting the property:
    ///     <ul>
    ///         <li>If the given view is not a valid view of the document; -or-</li><li>If the given view is a template view; -or-</li><li>If the given view is an internal view.</li>
    ///     </ul>
    /// </exception>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">
    ///     <para>
    ///         When setting the property:
    ///         <ul>
    ///             <li>If the document is not currently active; -or-</li><li>If the document is currently modifiable (i.e. with an active transaction); -or-</li>
    ///             <li>If the document is currently in read-only state; -or-</li><li>When invoked during either ViewActivating or ViewActivated event; -or-</li>
    ///             <li>When invoked during any pre-action kind of event, such as DocumentSaving, DocumentClosing, etc.</li>
    ///             <li>When there are no active documents in the current Autodesk Revit session</li>
    ///         </ul>
    ///     </para>
    /// </exception>
    public static View? ActiveView
    {
        get => UiApplication.ActiveUIDocument?.ActiveView;
        set
        {
            if (UiApplication.ActiveUIDocument is null) throw new InvalidOperationException("There are no active documents in the current Autodesk Revit session");
            UiApplication.ActiveUIDocument.ActiveView = value;
        }
    }

    /// <summary>Represents the currently active graphical view of the currently active document.</summary>
    /// <remarks>
    ///     This property is applicable to the currently active document only.
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </remarks>
    public static View? ActiveGraphicalView => UiApplication.ActiveUIDocument?.ActiveGraphicalView;

    /// <summary>
    ///     Determines whether Revit is in API mode or not.
    /// </summary>
    /// <remarks>
    ///     If Revit is within an API context, direct API calls should be used.
    ///     Otherwise, when Revit is outside the API context, API calls should be handled 
    ///     through the <see cref="Autodesk.Revit.UI.IExternalEventHandler"/> interface.
    ///     IExternalEventHandler enables safely executing commands and operations from external threads 
    ///     or the user interface, ensuring they are synchronized with Revit's main thread.
    /// </remarks>
    public static bool IsRevitInApiMode => GetIsRevitInApiMode();

    /// <summary>
    ///     Begins a scope that suppresses the display of Revit dialogs.
    ///     Dialogs are automatically restored when the returned scope is disposed.
    /// </summary>
    /// <param name="resultCode">The result code you wish the Revit dialog to return.</param>
    /// <returns>A disposable scope. Call Dispose or use 'using' statement to restore dialogs.</returns>
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
    ///     This method is thread-safe.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         using (RevitContext.BeginDialogSuppressionScope())
    ///         {
    ///             // Dialogs are suppressed here
    ///         }
    ///         // Dialogs are restored automatically
    ///     </code>
    /// </example>
    public static IDisposable BeginDialogSuppressionScope(int resultCode = 1)
    {
        lock (DialogLock)
        {
            if (_suppressDialogs)
            {
                _suppressDialogCode = resultCode;
                return new DialogSuppressionScope();
            }

            _suppressDialogs = true;
            _suppressDialogCode = resultCode;
            UiApplication.DialogBoxShowing += ResolveDialogBox;
        }

        return new DialogSuppressionScope();
    }

    /// <summary>
    ///     Begins a scope that suppresses the display of Revit dialogs with a custom handler.
    ///     Dialogs are automatically restored when the returned scope is disposed.
    /// </summary>
    /// <param name="handler">Suppress handler.</param>
    /// <returns>A disposable scope. Call Dispose or use 'using' statement to restore dialogs.</returns>
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
    ///     This method is thread-safe.
    /// </remarks>
    public static IDisposable BeginDialogSuppressionScope(Action<DialogBoxShowingEventArgs> handler)
    {
        lock (DialogLock)
        {
            if (_suppressDialogs)
            {
                _suppressDialogHandler = handler;
                return new DialogSuppressionScope();
            }

            _suppressDialogs = true;
            _suppressDialogHandler = handler;
            UiApplication.DialogBoxShowing += ResolveDialogBox;
        }

        return new DialogSuppressionScope();
    }

    private static void ResolveDialogBox(object? sender, DialogBoxShowingEventArgs args)
    {
        if (_suppressDialogCode.HasValue)
        {
            args.OverrideResult(_suppressDialogCode.Value);
            return;
        }

        _suppressDialogHandler?.Invoke(args);
    }

    private sealed class DialogSuppressionScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (DialogLock)
            {
                _suppressDialogs = false;
                _suppressDialogCode = null;
                _suppressDialogHandler = null;
                UiApplication.DialogBoxShowing -= ResolveDialogBox;
            }
        }
    }

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern UIControlledApplication CreateUiControlledApplication(UIApplication uiApplication);
#endif
}