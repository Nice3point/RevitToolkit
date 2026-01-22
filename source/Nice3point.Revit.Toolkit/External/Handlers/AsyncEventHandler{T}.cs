using System.Collections.Concurrent;
using System.ComponentModel;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

namespace Nice3point.Revit.Toolkit.External.Handlers;

/// <summary>
///     Handler to provide access to modify the Revit document asynchronously with a return value.
/// </summary>
[PublicAPI]
public sealed class AsyncEventHandler<T> : ExternalEventHandler
{
    private readonly ConcurrentQueue<(Func<UIApplication, T> Handler, TaskCompletionSource<T> Tcs, CancellationToken CancellationToken)> _queue = new();

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
                var result = item.Handler(uiApplication);
                item.Tcs.SetResult(result);
            }
            catch (Exception exception)
            {
                item.Tcs.SetException(exception);
            }
        }
    }

    /// <summary>
    ///     Instructing Revit to queue a handler, raise the external event and await completion with a result.
    /// </summary>
    /// <param name="handler">The function to execute in Revit context that returns a value.</param>
    /// <param name="cancellationToken">
    ///     Cancellation token to cancel the operation before it executes.
    ///     It cannot interrupt a handler that is already executing.
    /// </param>
    public Task<T> RaiseAsync(Func<UIApplication, T> handler, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        if (RevitContext.IsRevitInApiMode)
        {
            try
            {
                return Task.FromResult(handler(RevitContext.UiApplication));
            }
            catch (Exception exception)
            {
                return Task.FromException<T>(exception);
            }
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Enqueue((handler, tcs, cancellationToken));
        Raise();

        return tcs.Task;
    }
}