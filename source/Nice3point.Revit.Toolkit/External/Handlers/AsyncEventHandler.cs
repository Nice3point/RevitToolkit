using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Handler to provide access to modify the Revit document asynchronously.
/// </summary>
[PublicAPI]
public sealed class AsyncEventHandler : ExternalEventHandler
{
    private readonly ConcurrentQueue<(Action<UIApplication> Action, TaskCompletionSource Tcs, CancellationToken CancellationToken)> _queue = new();

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
                item.Tcs.SetResult();
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

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Enqueue((handler, tcs, cancellationToken));
        Raise();

        return tcs.Task;
    }
}