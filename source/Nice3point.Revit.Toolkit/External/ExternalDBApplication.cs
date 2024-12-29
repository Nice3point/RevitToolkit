using System.ComponentModel;
using Autodesk.Revit.ApplicationServices;
using Nice3point.Revit.Toolkit.Helpers;

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Class that supports addition of external applications to Revit. Is the entry point when loading an external application.
/// </summary>
/// <remarks>
///     External applications are permitted to customize the Revit UI, and to add events and updaters to the session.
/// </remarks>
[PublicAPI]
// ReSharper disable once InconsistentNaming
public abstract class ExternalDBApplication : IExternalDBApplication
{
#if NETCOREAPP
    private object? _isolatedInstance;
#endif
    /// <summary>
    ///     Indicates if the external application completes its work successfully.
    /// </summary>
    /// <remarks>
    ///     Method <see cref="OnShutdown()" /> will not be executed if the value of this property is different from <see cref="Autodesk.Revit.DB.ExternalDBApplicationResult.Succeeded" />.
    /// </remarks>
    public ExternalDBApplicationResult Result { get; set; } = ExternalDBApplicationResult.Succeeded;

    /// <summary>
    ///     Represents the Autodesk Revit Application with no access to documents.
    /// </summary>
    public ControlledApplication Application { get; private set; } = default!;

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ExternalDBApplicationResult OnStartup(ControlledApplication application)
    {
        var currentType = GetType();
        
        Application = application;
        
        try
        {
            ResolveHelper.BeginAssemblyResolve(currentType);
            OnStartup();
        }
        finally
        {
            ResolveHelper.EndAssemblyResolve();
        }

        return Result;
    }

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
    {
        var currentType = GetType();

        try
        {
            ResolveHelper.BeginAssemblyResolve(currentType);
            OnShutdown();
        }
        finally
        {
            ResolveHelper.EndAssemblyResolve();
        }

        return ExternalDBApplicationResult.Succeeded;
    }

    /// <summary>
    ///     Overload this method to execute some tasks when Revit starts.
    /// </summary>
    public abstract void OnStartup();

    /// <summary>
    ///     Overload this method to execute some tasks when Revit shuts down.
    /// </summary>
    /// <remarks>
    ///     The method will not be executed if the value of the <see cref="Result" /> property in the <see cref="OnStartup()" />
    ///     method is different from <see cref="Autodesk.Revit.DB.ExternalDBApplicationResult.Succeeded" />.
    /// </remarks>
    public virtual void OnShutdown()
    {
    }
}