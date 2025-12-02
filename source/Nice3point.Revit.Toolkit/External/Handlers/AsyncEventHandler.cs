using System.Collections.Concurrent;
using System.ComponentModel;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.External.Handlers;

/// <summary>
///     Handler to provide access to modify the Revit document asynchronously.
/// </summary>
[PublicAPI]
public sealed class AsyncEventHandler : ExternalEventHandler
{
#if NET
    private readonly ConcurrentQueue<(Action<UIApplication> Action, TaskCompletionSource Tcs)> _queue = new();
#else
    private readonly ConcurrentQueue<(Action<UIApplication> Action, TaskCompletionSource<object?> Tcs)> _queue = new();
#endif

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        while (_queue.TryDequeue(out var item))
        {
            try
            {
                item.Action(uiApplication);
#if NET
                item.Tcs.SetResult();
#else
                item.Tcs.SetResult(null);
#endif
            }
            catch (Exception exception)
            {
                item.Tcs.SetException(exception);
            }
        }
    }

    /// <summary>
    ///     Instructing Revit to queue a handler, raise the external event and await completion.
    /// </summary>
    public Task RaiseAsync(Action<UIApplication> handler)
    {
        if (Context.IsRevitInApiMode)
        {
            try
            {
                handler(Context.UiApplication);
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

#if NET
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
#else
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        _queue.Enqueue((handler, tcs));
        Raise();

        return tcs.Task;
    }
}