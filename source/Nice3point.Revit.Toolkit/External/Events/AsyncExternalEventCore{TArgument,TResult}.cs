using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
///     The owning event raises itself again while <see cref="HasPendingOperations" /> is <see langword="true" />, and each raise executes in its own event cycle.<br />
///     A raise whose token is canceled before its execution starts is canceled at once and never executes.
///     Once the execution starts, the cancellation of the token no longer affects the raise.
/// </remarks>
internal sealed class AsyncExternalEventCore<TArgument, TResult>(Func<UIApplication, TArgument, CancellationToken, TResult> handler)
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
    /// <param name="cancellationToken">The token to pass to the handler.</param>
    /// <returns>A completed task that contains the result or the exception of the handler, or a canceled task when the handler throws an <see cref="System.OperationCanceledException" /> for <paramref name="cancellationToken" />.</returns>
    public Task<TResult> Invoke(UIApplication uiApplication, TArgument argument, CancellationToken cancellationToken)
    {
        try
        {
            return Task.FromResult(handler(uiApplication, argument, cancellationToken));
        }
        catch (OperationCanceledException exception) when (IsCanceledBy(exception, cancellationToken))
        {
            return Task.FromCanceled<TResult>(cancellationToken);
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
    /// <param name="cancellationToken">The token that withdraws the raise before its execution, passed to the handler.</param>
    /// <returns>A task that completes with the outcome of the handler once <see cref="InvokeNext" /> executes the raise, or a task canceled at the cancellation of <paramref name="cancellationToken" /> before the execution.</returns>
    public Task<TResult> Enqueue(TArgument argument, CancellationToken cancellationToken)
    {
        var operation = new Operation(argument, cancellationToken);
        _operations.Enqueue(operation);

        return operation.Task;
    }

    /// <summary>
    ///     Executes the handler for the oldest raise in the queue that is not canceled and completes its task.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication" /> instance.</param>
    /// <remarks>A queue without a raise to execute executes nothing.</remarks>
    public void InvokeNext(UIApplication uiApplication)
    {
        if (!TryStartNext(out var operation))
        {
            return;
        }

        try
        {
            operation.SetResult(handler(uiApplication, operation.Argument, operation.CancellationToken));
        }
        catch (Exception exception)
        {
            operation.SetException(exception);
        }
    }

    private bool TryStartNext([NotNullWhen(true)] out Operation? operation)
    {
        while (_operations.TryDequeue(out operation))
        {
            if (operation.TryStart())
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCanceledBy(OperationCanceledException exception, CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested && exception.CancellationToken == cancellationToken;
    }

    private sealed class Operation
    {
        private const int Pending = 0;
        private const int Started = 1;
        private const int Canceled = 2;

        private readonly TaskCompletionSource<TResult> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenRegistration _cancellationRegistration;
        private int _state;

        public Operation(TArgument argument, CancellationToken cancellationToken)
        {
            Argument = argument;
            CancellationToken = cancellationToken;
            _cancellationRegistration = cancellationToken.Register(static state => ((Operation)state!).Cancel(), this);
        }

        public TArgument Argument { get; private set; }
        public CancellationToken CancellationToken { get; }
        public Task<TResult> Task => _completionSource.Task;

        public bool TryStart()
        {
            if (Interlocked.CompareExchange(ref _state, Started, Pending) != Pending)
            {
                return false;
            }

            _cancellationRegistration.Dispose();
            return true;
        }

        public void SetResult(TResult result)
        {
            _completionSource.TrySetResult(result);
        }

        public void SetException(Exception exception)
        {
            if (exception is OperationCanceledException canceledException && IsCanceledBy(canceledException, CancellationToken))
            {
                _completionSource.TrySetCanceled(CancellationToken);
                return;
            }

            _completionSource.TrySetException(exception);
        }

        private void Cancel()
        {
            if (Interlocked.CompareExchange(ref _state, Canceled, Pending) != Pending)
            {
                return;
            }

            Argument = default!;
            _completionSource.TrySetCanceled(CancellationToken);
        }
    }
}
