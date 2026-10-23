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
/// <param name="raise">The function that raises the Revit external event.</param>
/// <remarks>
///     Revit runs a pending external event once however often it is raised.
///     Each execution runs the oldest raise and raises the event again for the rest.
///     When Revit refuses that raise, the rest runs in the same execution.<br />
///     The execution, the cancellation, and the refusal of a raise compete for the reservation of its completion.
///     Only the one that reserves it completes the task.
/// </remarks>
internal sealed class AsyncExternalEventCore<TArgument, TResult>(Func<UIApplication, TArgument, CancellationToken, TResult> handler, Func<ExternalEventRequest> raise)
{
    private readonly ConcurrentQueue<Operation> _operations = new();

    /// <summary>
    ///     Executes the handler on the calling thread.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication" /> instance.</param>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <param name="cancellationToken">The token to pass to the handler.</param>
    /// <returns>A completed task that contains the outcome of the handler, or a canceled task when <paramref name="cancellationToken" /> is canceled before the handler executes.</returns>
    public Task<TResult> Invoke(UIApplication uiApplication, TArgument argument, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TResult>(cancellationToken);
        }

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
    ///     Adds a raise to the end of the queue and raises the Revit external event.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <param name="cancellationToken">The token that withdraws the raise before its execution, passed to the handler.</param>
    /// <returns>A task that completes with the outcome of the handler, or a task canceled at the cancellation of <paramref name="cancellationToken" /> before the execution.</returns>
    /// <remarks>A raise that Revit refuses fails its task with an <see cref="System.InvalidOperationException" /> without executing.</remarks>
    public Task<TResult> RaiseAsync(TArgument argument, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TResult>(cancellationToken);
        }

        var operation = new Operation(argument, cancellationToken);
        _operations.Enqueue(operation);

        var request = raise();
        if (request.IsRefused)
        {
            operation.TrySetException(new InvalidOperationException($"Revit did not accept the external event request and returned the {request} result. Raise the external event again."));
        }

        return operation.Task;
    }

    /// <summary>
    ///     Executes the oldest raise in the queue that is not canceled and raises the Revit external event again for the rest.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication" /> instance.</param>
    public void Execute(UIApplication uiApplication)
    {
        while (TryDequeueAndReserveCompletion(out var operation))
        {
            var executesNextRaise = !_operations.IsEmpty && raise().IsRefused;

            try
            {
                operation.DangerousSetResult(handler(uiApplication, operation.Argument, operation.CancellationToken));
            }
            catch (Exception exception)
            {
                operation.DangerousSetException(exception);
            }

            if (!executesNextRaise)
            {
                return;
            }
        }
    }

    private bool TryDequeueAndReserveCompletion([NotNullWhen(true)] out Operation? operation)
    {
        while (_operations.TryDequeue(out operation))
        {
            if (operation.TryReserveCompletion())
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
        private readonly TaskCompletionSource<TResult> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenRegistration _cancellationRegistration;
        private int _completionReserved;

        public Operation(TArgument argument, CancellationToken cancellationToken)
        {
            Argument = argument;
            CancellationToken = cancellationToken;
            _cancellationRegistration = cancellationToken.Register(static state => ((Operation)state!).TrySetCanceled(), this);
        }

        public TArgument Argument { get; private set; }
        public CancellationToken CancellationToken { get; }
        public Task<TResult> Task => _completionSource.Task;

        public bool TryReserveCompletion()
        {
            if (Interlocked.Exchange(ref _completionReserved, 1) != 0)
            {
                return false;
            }

            _cancellationRegistration.Dispose();
            return true;
        }

        public bool TrySetException(Exception exception)
        {
            if (!TryReserveCompletion())
            {
                return false;
            }

            Argument = default!;
            _completionSource.TrySetException(exception);
            return true;
        }

        public void DangerousSetResult(TResult result)
        {
            _completionSource.TrySetResult(result);
        }

        public void DangerousSetException(Exception exception)
        {
            if (exception is OperationCanceledException canceledException && IsCanceledBy(canceledException, CancellationToken))
            {
                _completionSource.TrySetCanceled(CancellationToken);
                return;
            }

            _completionSource.TrySetException(exception);
        }

        private void TrySetCanceled()
        {
            if (!TryReserveCompletion())
            {
                return;
            }

            Argument = default!;
            _completionSource.TrySetCanceled(CancellationToken);
        }
    }
}
