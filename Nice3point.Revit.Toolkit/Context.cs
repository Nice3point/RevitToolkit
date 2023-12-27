using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit;

/// <summary>
///     Provides members for setting and retrieving data about Revit application context.
/// </summary>
[PublicAPI]
public static class Context
{
    /// <summary>
    ///     Represents an active session of the Autodesk Revit user interface, providing access to
    ///     UI customization methods, events, the main window, and the active document.
    /// </summary>
    /// <remarks>
    ///     You can access documents from the database level Application object, obtained from
    ///     the Application property.  If you have an instance of the database level Application object,
    ///     you can construct a UIApplication instance from it.
    /// </remarks>
    public static UIApplication UiApplication { get; }

    /// <summary>
    ///     Represents the Autodesk Revit Application, providing access to documents, options and other application wide data and settings.
    /// </summary>
    public static Application Application { get; }

    /// <summary>
    ///     An object that represents an Autodesk Revit project opened in the Revit user interface.
    /// </summary>
    /// <remarks>
    ///     <p>
    ///         This class represents a document opened in the user interface and therefore offers interfaces
    ///         to work with settings and operations in the UI (for example, the active selection). Revit can have multiple
    ///         projects open and multiple views to those projects. The active or top most view will be the
    ///         active project and hence the active document which is available from the UIApplication object.
    ///     </p>
    ///     Obtain the database level Document (which contains interfaces not related to the UI) via the
    ///     Document property.  If you have a database level Document and need to access it from the UI, you can
    ///     construct a new UIDocument from that object (the document must be open and visible in the UI to allow the methods to
    ///     work successfully).
    /// </remarks>
    [CanBeNull]
    public static UIDocument UiDocument => UiApplication.ActiveUIDocument;

    /// <summary>An object that represents an open Autodesk Revit project.</summary>
    /// <remarks>
    ///     The Document object represents an Autodesk Revit project. Revit can have multiple
    ///     projects open and multiple views to those projects. The active or top most view will be the
    ///     active project and hence the active document which is available from the Application object.
    /// </remarks>
    /// <exception cref="System.NullReferenceException">When UiDocument is null</exception>
    public static Document Document => UiApplication.ActiveUIDocument.Document;

    /// <summary>The currently active view of the currently active document.</summary>
    /// <since>2012</since>
    /// <remarks>
    ///     <para>
    ///         This property is applicable to the currently active document only.
    ///         Returns <see langword="null" /> if this document doesn't represent the active document.
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
    ///         </ul>
    ///     </para>
    /// </exception>
    /// <exception cref="System.NullReferenceException">When UiDocument is null</exception>
    public static View ActiveView
    {
        get => UiApplication.ActiveUIDocument.ActiveView;
        set => UiApplication.ActiveUIDocument.ActiveView = value;
    }

    /// <summary>The currently active graphical view of the currently active document.</summary>
    /// <remarks>
    ///     This property is applicable to the currently active document only.
    ///     Returns <see langword="null" /> if this document doesn't represent the active document.
    /// </remarks>
    /// <exception cref="System.NullReferenceException">When UiDocument is null</exception>
    public static View ActiveGraphicalView => UiApplication.ActiveUIDocument.ActiveGraphicalView;

    static Context()
    {
        const BindingFlags staticFlags = BindingFlags.NonPublic | BindingFlags.Static;
        var dbAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "RevitDBAPI");
        ThrowIfNotSupported(dbAssembly);

        var getApplicationMethod = dbAssembly.ManifestModule.GetMethods(staticFlags).First(info => info.Name == "RevitApplication.getApplication_");
        ThrowIfNotSupported(getApplicationMethod);

        var proxyType = dbAssembly.DefinedTypes.FirstOrDefault(info => info.FullName == "Autodesk.Revit.Proxy.ApplicationServices.ApplicationProxy");
        ThrowIfNotSupported(proxyType);

        const BindingFlags internalFlags = BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
        var proxyConstructor = proxyType.GetConstructor(internalFlags, null, [getApplicationMethod.ReturnType], null);
        ThrowIfNotSupported(proxyConstructor);

        var proxy = proxyConstructor.Invoke([getApplicationMethod.Invoke(null, null)]);
        ThrowIfNotSupported(proxy);

        var applicationType = typeof(Application);
        var applicationConstructor = applicationType.GetConstructor(internalFlags, null, [proxyType], null);
        ThrowIfNotSupported(applicationConstructor);

        var application = (Application) applicationConstructor.Invoke([proxy]);
        ThrowIfNotSupported(proxy);

        Application = application;
        UiApplication = new UIApplication(application);
    }

    [ContractAnnotation("argument:null=> halt")]
    private static void ThrowIfNotSupported([CanBeNull] object argument)
    {
        if (argument is null)
        {
            throw new NotSupportedException("The operation is not supported by current Revit API version. Failed to retrieve application context.");
        }
    }
}