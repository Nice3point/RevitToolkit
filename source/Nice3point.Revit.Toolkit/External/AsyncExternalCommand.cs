using System.Windows.Threading;

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Implementation for an asynchronous Revit <see cref="Autodesk.Revit.UI.IExternalCommand"/>.
/// </summary>
[PublicAPI]
public abstract class AsyncExternalCommand : ExternalCommand
{
    /// <summary>
    ///     Overload this method to implement an asynchronous external command within Revit.
    /// </summary>
    /// <remarks>
    ///     This method is executed on the main thread, but allows using await for I/O operations.<br/>
    ///     The Revit UI will remain blocked until this method completes.<br/>
    ///     as it will cause a deadlock (the handler waits for Idle, but Revit is blocked waiting for this command).
    /// </remarks>
    public abstract Task ExecuteAsync();

    /// <summary>
    ///     Synchronous execution wrapper.
    /// </summary>
    public sealed override void Execute()
    {
        var task = ExecuteAsync();
        if (task.IsCompleted)
        {
            //Rethrow exceptions
            task.GetAwaiter().GetResult();
            return;
        }

        var frame = new DispatcherFrame();
        // Ensure the continuation runs on the ThreadPool to avoid deadlocks on the UI thread
        task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);

        Dispatcher.PushFrame(frame);

        //Rethrow exceptions
        task.GetAwaiter().GetResult();
    }
}