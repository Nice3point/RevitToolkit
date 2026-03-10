using System.Windows.Threading;
using JetBrains.Annotations;

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
    ///     The method will not be executed if the value of the <see cref="Autodesk.Revit.UI.Result" /> property in the <see cref="OnStartupAsync()" />
    ///     method is different from <see cref="Autodesk.Revit.UI.Result.Succeeded" />.
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
        ExecuteAsync(OnStartupAsync);
    }

    /// <summary>
    ///     Synchronous execution wrapper with message pumping.
    /// </summary>
    public sealed override void OnShutdown()
    {
        ExecuteAsync(OnShutdownAsync);
    }

    private static void ExecuteAsync(Func<Task> asyncMethod)
    {
        var task = asyncMethod();
        if (task.IsCompleted)
        {
            //Rethrow exceptions
            task.GetAwaiter().GetResult();
            return;
        }

        var frame = new DispatcherFrame();

        // TaskScheduler.Default ensures continuation runs on ThreadPool, not UI thread.
        // Prevents deadlock: if continuation ran on UI thread via SynchronizationContext,
        // it would wait for PushFrame to finish, which waits for continuation - deadlock.
        task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);

        Dispatcher.PushFrame(frame);

        task.GetAwaiter().GetResult();
    }
}