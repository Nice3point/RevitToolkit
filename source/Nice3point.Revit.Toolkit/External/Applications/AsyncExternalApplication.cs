using Nice3point.Revit.Toolkit.Internal;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Implementation for an asynchronous Revit <see cref="Autodesk.Revit.UI.IExternalApplication" />.
/// </summary>
/// <remarks>
///     <para>
///         Enables async/await patterns within Revit external applications while maintaining
///         execution on the Revit main thread. The Revit UI remains responsive during async operations
///         through dispatcher message pumping.
///     </para>
///     <para>
///         Ideal for I/O-bound operations such as HTTP requests, file operations, or database queries
///         during application startup and shutdown.
///         Revit API calls can be made before and after await points since execution resumes on the main thread.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     public override async Task OnStartupAsync()
///     {
///         using var httpClient = new HttpClient();
///         var configuration = await httpClient.GetStringAsync("https://api.example.com/config");
///         
///         var panel = Application.CreatePanel(configuration.PanelTitle);
///         panel.AddPushButton&lt;Command&gt;(configuration.ButtonTitle);
///     }
///     </code>
/// </example>
[PublicAPI]
public abstract class AsyncExternalApplication : ExternalApplication
{
    /// <summary>
    ///     Overload this method to execute some asynchronous tasks when Revit starts.
    /// </summary>
    public abstract Task OnStartupAsync();

    /// <summary>
    ///     Overload this method to execute some asynchronous tasks when Revit shuts down.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The method will not be executed if the value of the <see cref="Autodesk.Revit.UI.Result" /> property in the <see cref="OnStartupAsync()" />
    ///         method is different from <see cref="Autodesk.Revit.UI.Result.Succeeded" />.
    ///     </para>
    ///     <para>
    ///         Revit shuts down the WPF dispatcher of its thread after the main window unloads, which can precede this method.
    ///         The code after an <see langword="await" /> runs on the Revit thread in both states of the dispatcher.
    ///         A dispatcher that has shut down cancels every operation posted to it.
    ///     </para>
    /// </remarks>
    public virtual Task OnShutdownAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Synchronous execution wrapper with message pumping.
    /// </summary>
    public sealed override void OnStartup()
    {
        MessagePump.Run(OnStartupAsync);
    }

    /// <summary>
    ///     Synchronous execution wrapper with message pumping.
    /// </summary>
    public sealed override void OnShutdown()
    {
        MessagePump.Run(OnShutdownAsync);
    }
}
