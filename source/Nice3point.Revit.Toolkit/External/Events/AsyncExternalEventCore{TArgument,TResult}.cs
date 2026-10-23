using System.Collections.Concurrent;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Provides the queue of raises of an asynchronous external event, the execution of each raise, and the completion of its task.
/// </summary>
/// <typeparam name="TArgument">The type of the argument passed to the handler.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the handler.</typeparam>
/// <param name="handler">The execution logic of the external event.</param>
/// <remarks>
///     Revit runs a pending external event once however often it is raised.
///     The owning event raises itself again while <see cref="HasPendingOperations" /> is <see langword="true" />, and each raise executes in its own event cycle.
/// </remarks>
internal sealed class AsyncExternalEventCore<TArgument, TResult>(Func<UIApplication, TArgument, TResult> handler)
{
    private readonly ConcurrentQueue<Operation> _operations = new();

    /// <summary>
    ///     Gets a value indicating whether a raise waits for its execution.
    /// </summary>
    public bool HasPendingOperations => !_operations.IsEmpty;

    /// <summary>
    ///     Executes the handler on the calling thread.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication" /> instance.</param>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>A completed task that contains the result or the exception of the handler.</returns>
    public Task<TResult> Invoke(UIApplication uiApplication, TArgument argument)
    {
        try
        {
            return Task.FromResult(handler(uiApplication, argument));
        }
        catch (Exception exception)
        {
            return Task.FromException<TResult>(exception);
        }
    }

    /// <summary>
    ///     Adds a raise to the end of the queue.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>A task that completes with the result or the exception of the handler once <see cref="InvokeNext" /> executes the raise.</returns>
    public Task<TResult> Enqueue(TArgument argument)
    {
        var operation = new Operation(argument);
        _operations.Enqueue(operation);

        return operation.Task;
    }

    /// <summary>
    ///     Executes the handler for the oldest raise in the queue and completes its task.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication" /> instance.</param>
    /// <remarks>An empty queue executes nothing.</remarks>
    public void InvokeNext(UIApplication uiApplication)
    {
        if (!_operations.TryDequeue(out var operation))
        {
            return;
        }

        try
        {
            operation.SetResult(handler(uiApplication, operation.Argument));
        }
        catch (Exception exception)
        {
            operation.SetException(exception);
        }
    }

    private sealed class Operation(TArgument argument)
    {
        private readonly TaskCompletionSource<TResult> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TArgument Argument { get; } = argument;
        public Task<TResult> Task => _completionSource.Task;

        public void SetResult(TResult result)
        {
            _completionSource.TrySetResult(result);
        }

        public void SetException(Exception exception)
        {
            _completionSource.TrySetException(exception);
        }
    }
}
