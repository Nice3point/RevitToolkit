using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Nice3point.Revit.Toolkit;

/// <summary>
///     Provides members for accessing the Revit application context at the UI level.
/// </summary>
[PublicAPI]
public class RevitContext : RevitApiContext
{
    private static readonly Func<bool> GetIsRevitInApiMode;
    private static readonly IntPtr IncrementConstructorPointer;
    private static readonly IntPtr IncrementDestructorPointer;

    private static readonly Lock DialogLock = new();
    private static int _dialogScopeCount;
    private static int? _suppressDialogCode;
    private static Action<DialogBoxShowingEventArgs>? _suppressDialogHandler;

    static RevitContext()
    {
        var assemblies = FindAssemblies("APIUIAPI", "RevitAPIUI");

        var apiAssemblyMethods = assemblies[0].ManifestModule.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
        var apiCallDepthManagerMethod = apiAssemblyMethods.FirstOrDefault(static method => method.Name == "APICallDepthManager.singletonfactory");
        ThrowWhen(apiCallDepthManagerMethod is null);

        var isRevitInApiModeMethod = apiAssemblyMethods.FirstOrDefault(static method => method.Name == "APICallDepthManager.isRevitInAPIMode");
        ThrowWhen(isRevitInApiModeMethod is null);

        var uiAssemblyMethods = assemblies[1].ManifestModule.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
        var incrementConstructor = uiAssemblyMethods.FirstOrDefault(static method => method.Name == "IncrementAPICallDepth.{ctor}");
        ThrowWhen(incrementConstructor is null);

        var incrementDestructor = uiAssemblyMethods.FirstOrDefault(static method => method.Name == "IncrementAPICallDepth.{dtor}");
        ThrowWhen(incrementDestructor is null);

        IncrementConstructorPointer = incrementConstructor.MethodHandle.GetFunctionPointer();
        IncrementDestructorPointer = incrementDestructor.MethodHandle.GetFunctionPointer();

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

    /// <summary>Represents a currently active Autodesk Revit project at the UI level.</summary>
    /// <remarks>
    ///     External API commands can access this property in read-only mode only.
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">Thrown when attempting to modify the property.</exception>
    /// <returns>
    ///     Currently active project.<br />
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </returns>
    public static UIDocument? ActiveUiDocument => UiApplication.ActiveUIDocument;

    /// <summary>Represents a currently active Autodesk Revit project at the database level.</summary>
    /// <remarks>
    ///     Revit can have multiple projects open and multiple views to those projects.
    ///     The active or top most view will be the active project and hence the active document which is available from the Application object.<br /><br />
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </remarks>
    public static Document? ActiveDocument => UiApplication.ActiveUIDocument?.Document;

    /// <summary>Represents the currently active view of the currently active document.</summary>
    /// <remarks>
    ///     <para>
    ///         This property is applicable to the currently active document only.<br />
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
            if (UiApplication.ActiveUIDocument is null)
            {
                throw new InvalidOperationException("There are no active documents in the current Autodesk Revit session");
            }

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
    ///     through the <see cref="Autodesk.Revit.UI.IExternalEventHandler" /> interface.
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
            _suppressDialogCode = resultCode;
            _suppressDialogHandler = null;

            if (_dialogScopeCount++ == 0)
            {
                UiApplication.DialogBoxShowing += ResolveDialogBox;
            }
        }

        return new DialogSuppressionScope();
    }

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
    ///             MessageBox: Standard Message Box IDs, such as IDOK and IDCANCEL, are accepted.
    ///             For all possible IDs, refer to the Windows API documentation.
    ///             The ID used must be relevant to the buttons in a message box.
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
    public static IDisposable BeginDialogSuppressionScope(MessageBoxResult resultCode)
    {
        lock (DialogLock)
        {
            _suppressDialogCode = (int)resultCode;
            _suppressDialogHandler = null;

            if (_dialogScopeCount++ == 0)
            {
                UiApplication.DialogBoxShowing += ResolveDialogBox;
            }
        }

        return new DialogSuppressionScope();
    }

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
    public static IDisposable BeginDialogSuppressionScope(TaskDialogResult resultCode)
    {
        lock (DialogLock)
        {
            _suppressDialogCode = (int)resultCode;
            _suppressDialogHandler = null;

            if (_dialogScopeCount++ == 0)
            {
                UiApplication.DialogBoxShowing += ResolveDialogBox;
            }
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
            _suppressDialogCode = null;
            _suppressDialogHandler = handler;

            if (_dialogScopeCount++ == 0)
            {
                UiApplication.DialogBoxShowing += ResolveDialogBox;
            }
        }

        return new DialogSuppressionScope();
    }

    internal static IDisposable BeginApiContextScope()
    {
        return new ApiContextScope(IncrementConstructorPointer, IncrementDestructorPointer);
    }

    private static void ResolveDialogBox(object? sender, DialogBoxShowingEventArgs args)
    {
        int? code;
        Action<DialogBoxShowingEventArgs>? handler;

        lock (DialogLock)
        {
            code = _suppressDialogCode;
            handler = _suppressDialogHandler;
        }

        if (code.HasValue)
        {
            args.OverrideResult(code.Value);
            return;
        }

        handler?.Invoke(args);
    }

    private sealed class ApiContextScope : IDisposable
    {
        private readonly IntPtr _deconstructorPointer;
        private readonly IntPtr _memory;
        private int _disposed;

        internal ApiContextScope(IntPtr constructorPointer, IntPtr deconstructorPointer)
        {
            _deconstructorPointer = deconstructorPointer;
            _memory = Marshal.AllocHGlobal(8);
            Marshal.WriteInt64(_memory, 0);

            var constructorDelegate = Marshal.GetDelegateForFunctionPointer<IncrementCtor>(constructorPointer);
            constructorDelegate(_memory);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            var deconstructorDelegate = Marshal.GetDelegateForFunctionPointer<IncrementDtor>(_deconstructorPointer);
            deconstructorDelegate(_memory);

            Marshal.FreeHGlobal(_memory);
        }
    }

    private sealed class DialogSuppressionScope : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            lock (DialogLock)
            {
                if (--_dialogScopeCount == 0)
                {
                    _suppressDialogCode = null;
                    _suppressDialogHandler = null;
                    UiApplication.DialogBoxShowing -= ResolveDialogBox;
                }
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr IncrementCtor(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void IncrementDtor(IntPtr self);
}
