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
    private readonly ConcurrentQueue<(Action<UIApplication> Action, TaskCompletionSource Tcs, CancellationToken CancellationToken)> _queue = new();
#else
    private readonly ConcurrentQueue<(Action<UIApplication> Action, TaskCompletionSource<object?> Tcs, CancellationToken CancellationToken)> _queue = new();
#endif

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        while (_queue.TryDequeue(out var item))
        {
            if (item.CancellationToken.IsCancellationRequested)
            {
                item.Tcs.SetCanceled(item.CancellationToken);
                continue;
            }

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
    /// <param name="handler">The action to execute in Revit context.</param>
    /// <param name="cancellationToken">
    ///     Cancellation token to cancel the operation before it executes.
    ///     It cannot interrupt a handler that is already executing.
    /// </param>
    public Task RaiseAsync(Action<UIApplication> handler, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (RevitContext.IsRevitInApiMode)
        {
            try
            {
                handler(RevitContext.UiApplication);
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
        _queue.Enqueue((handler, tcs, cancellationToken));
        Raise();

        return tcs.Task;
    }
}