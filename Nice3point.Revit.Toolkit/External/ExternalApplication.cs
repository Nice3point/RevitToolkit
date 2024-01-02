using System.ComponentModel;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.Helpers;

namespace Nice3point.Revit.Toolkit.External;

/// <summary>Class that supports addition of external applications to Revit. Is the entry point when loading an external application</summary>
/// <remarks>
///     External applications are permitted to customize the Revit UI, and to add events and updaters to the session
/// </remarks>
[PublicAPI]
public abstract class ExternalApplication : IExternalApplication
{
    /// <summary>
    ///     Indicates if the external application completes its work successfully.
    /// </summary>
    /// <remarks>
    ///     Method <see cref="OnShutdown()" /> will not be executed if the value of this property is different from <see cref="Autodesk.Revit.UI.Result.Succeeded" />
    /// </remarks>
    public Result Result { get; set; } = Result.Succeeded;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.UIControlledApplication" /> that is needed by an external application
    /// </summary>
    public UIControlledApplication Application { get; private set; }

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.UIApplication" /> that is needed by an external application
    /// </summary>
    public UIApplication UiApplication => Context.UiApplication;

    /// <summary>Implement this method to execute some tasks when Autodesk Revit starts</summary>
    /// <param name="application">A handle to the application being started</param>
    /// <returns>Indicates if the external application completes its work successfully</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result OnStartup(UIControlledApplication application)
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
    public Result OnShutdown(UIControlledApplication application)
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

        return Result.Succeeded;
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
    ///     method is different from <see cref="Autodesk.Revit.UI.Result.Succeeded" />
    /// </remarks>
    public virtual void OnShutdown()
    {
    }
}