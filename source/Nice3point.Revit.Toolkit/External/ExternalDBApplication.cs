using System.ComponentModel;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Nice3point.Revit.Toolkit.Helpers;

namespace Nice3point.Revit.Toolkit.External;

/// <summary>Class that supports addition of external applications to Revit. Is the entry point when loading an external application</summary>
/// <remarks>
///     External applications are permitted to customize the Revit UI, and to add events and updaters to the session
/// </remarks>
[PublicAPI]
// ReSharper disable once InconsistentNaming
public abstract class ExternalDBApplication : IExternalDBApplication
{
    /// <summary>
    ///     Indicates if the external application completes its work successfully.
    /// </summary>
    /// <remarks>
    ///     Method <see cref="OnShutdown()" /> will not be executed if the value of this property is different from <see cref="Autodesk.Revit.DB.ExternalDBApplicationResult.Succeeded" />
    /// </remarks>
    public ExternalDBApplicationResult Result { get; set; } = ExternalDBApplicationResult.Succeeded;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.ApplicationServices.ControlledApplication" /> that is needed by an external application
    /// </summary>
    public ControlledApplication Application { get; private set; }

    /// <summary>Implement this method to execute some tasks when Autodesk Revit starts</summary>
    /// <param name="application">A handle to the application being started</param>
    /// <returns>Indicates if the external application completes its work successfully</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ExternalDBApplicationResult OnStartup(ControlledApplication application)
    {
        Application = application;
        try
        {
            ResolveHelper.BeginAssemblyResolve(GetType());
            OnStartup();
        }
        finally
        {
            ResolveHelper.EndAssemblyResolve();
        }

        return Result;
    }

    /// <summary>Implement this method to execute some tasks when Autodesk Revit shuts down</summary>
    /// <param name="application">A handle to the application being shut down</param>
    /// <returns>Indicates if the external application completes its work successfully</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
    {
        try
        {
            ResolveHelper.BeginAssemblyResolve(GetType());
            OnShutdown();
        }
        finally
        {
            ResolveHelper.EndAssemblyResolve();
        }

        return ExternalDBApplicationResult.Succeeded;
    }

    /// <summary>
    ///     Overload this method to execute some tasks when Revit starts
    /// </summary>
    public abstract void OnStartup();

    /// <summary>
    ///     Overload this method to execute some tasks when Revit shuts down
    /// </summary>
    /// <remarks>
    ///     The method will not be executed if the value of the <see cref="Result" /> property in the <see cref="OnStartup()" />
    ///     method is different from <see cref="Autodesk.Revit.DB.ExternalDBApplicationResult.Succeeded" />
    /// </remarks>
    public virtual void OnShutdown()
    {
    }
}