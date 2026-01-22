using Autodesk.Revit.UI;
using JetBrains.Annotations;

namespace Nice3point.Revit.Toolkit.Decorators;

/// <summary>
///     Provides access to create a new dockable pane to the Revit user interface.
/// </summary>
[PublicAPI]
public class DockablePaneProvider : IDockablePaneProvider, IDockablePaneProviderId, IDockablePaneProviderTitle, IDockablePaneProviderConfiguration
{
#nullable disable //Nullable values controlled by Fluent API
    private UIControlledApplication _application;
    private DockablePaneId _id;
    private Action<DockablePaneProviderData> _setupHandler;
    private string _title;
#nullable restore

    private DockablePaneProvider()
    {
    }

    /// <summary>
    ///     Method called during initialization of the user interface to gather information about a dockable pane window.
    /// </summary>
    /// <param name="data">Container for information about the new dockable pane.</param>
    public void SetupDockablePane(DockablePaneProviderData data)
    {
        _setupHandler(data);
    }

    void IDockablePaneProviderConfiguration.SetConfiguration(Action<DockablePaneProviderData> handler)
    {
        _setupHandler = handler;
        _application.RegisterDockablePane(_id, _title, this);
    }

    IDockablePaneProviderTitle IDockablePaneProviderId.SetId(Guid id)
    {
        _id = new DockablePaneId(id);
        return this;
    }

    IDockablePaneProviderTitle IDockablePaneProviderId.SetId(DockablePaneId id)
    {
        _id = id;
        return this;
    }

    IDockablePaneProviderConfiguration IDockablePaneProviderTitle.SetTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    ///     Creates a new <see cref="IDockablePaneProvider" /> instance.
    /// </summary>
    /// <param name="application">The UIControlledApplication.</param>
    public static IDockablePaneProviderId Register(UIControlledApplication application)
    {
        return new DockablePaneProvider
        {
            _application = application
        };
    }

    /// <summary>
    ///     Creates a new <see cref="IDockablePaneProvider" /> instance.
    /// </summary>
    /// <param name="application">The UIControlledApplication.</param>
    /// <param name="id">Unique identifier for the new pane.</param>
    /// <param name="title">String to use for the pane caption.</param>
    public static IDockablePaneProviderConfiguration Register(UIControlledApplication application, Guid id, string title)
    {
        return new DockablePaneProvider
        {
            _application = application,
            _id = new DockablePaneId(id),
            _title = title
        };
    }

    /// <summary>
    ///     Creates a new <see cref="IDockablePaneProvider" /> instance.
    /// </summary>
    /// <param name="application">The UIControlledApplication.</param>
    /// <param name="id">Unique identifier for the new pane.</param>
    /// <param name="title">String to use for the pane caption.</param>
    public static IDockablePaneProviderConfiguration Register(UIControlledApplication application, DockablePaneId id, string title)
    {
        return new DockablePaneProvider
        {
            _application = application,
            _id = id,
            _title = title
        };
    }
}

/// <summary>
///     Interface for the data passed to the <see cref="UIControlledApplication.RegisterDockablePane" /> method.
/// </summary>
[PublicAPI]
public interface IDockablePaneProviderTitle
{
    /// <summary>
    ///     Sets the title of the dockable pane.
    /// </summary>
    /// <param name="title">String to use for the pane caption.</param>
    IDockablePaneProviderConfiguration SetTitle(string title);
}

/// <summary>
///     Interface for the data passed to the <see cref="UIControlledApplication.RegisterDockablePane" /> method.
/// </summary>
[PublicAPI]
public interface IDockablePaneProviderId
{
    /// <summary>
    ///     Sets the Id of the dockable pane.
    /// </summary>
    /// <param name="id">Unique identifier for the new pane.</param>
    IDockablePaneProviderTitle SetId(Guid id);

    /// <summary>
    ///     Sets the Id of the dockable pane.
    /// </summary>
    /// <param name="id">Unique identifier for the new pane.</param>
    IDockablePaneProviderTitle SetId(DockablePaneId id);
}

/// <summary>
///     Interface that the Revit UI will call during initialization of the user interface to gather information about add-in dockable pane windows.
/// </summary>
[PublicAPI]
public interface IDockablePaneProviderConfiguration
{
    /// <summary>
    ///     Sets the configuration of the dockable pane.
    /// </summary>
    /// <param name="handler">Configuration handler.
    ///     Provides a container for information about the new dockable pane.
    ///     Implementers should set the FrameworkElement and InitialState Properties.
    ///     Optionally, providers can set the ContextualHelp property if they wish to provide or react to help requests on the pane,
    ///     or override the default EditorInteraction property by setting it here.
    /// </param>
    void SetConfiguration(Action<DockablePaneProviderData> handler);
}