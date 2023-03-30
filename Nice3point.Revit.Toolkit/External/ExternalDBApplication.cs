using System.ComponentModel;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Nice3point.Revit.Toolkit.Helpers;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

namespace Nice3point.Revit.Toolkit.External;

/// <summary>Class that supports addition of external applications to Revit. Is the entry point when loading an external application</summary>
/// <remarks>
///     External applications are permitted to customize the Revit UI, and to add events and updaters to the session
/// </remarks>
[PublicAPI]
// ReSharper disable once InconsistentNaming
public abstract class ExternalDBApplication : IExternalDBApplication
{
    private string _callerAssemblyDirectory;

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
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyOnStartup;
        try
        {
            OnStartup();
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssemblyOnStartup;
        }

        return Result;
    }

    /// <summary>Implement this method to execute some tasks when Autodesk Revit shuts down</summary>
    /// <param name="application">A handle to the application being shut down</param>
    /// <returns>Indicates if the external application completes its work successfully</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyOnShutdown;
        try
        {
            OnShutdown();
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssemblyOnShutdown;
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

    private Assembly ResolveAssemblyOnStartup(object sender, ResolveEventArgs args)
    {
        return ResolveHelper.ResolveAssembly(nameof(OnStartup), args, ref _callerAssemblyDirectory);
    }

    private Assembly ResolveAssemblyOnShutdown(object sender, ResolveEventArgs args)
    {
        return ResolveHelper.ResolveAssembly(nameof(OnShutdown), args, ref _callerAssemblyDirectory);
    }
}