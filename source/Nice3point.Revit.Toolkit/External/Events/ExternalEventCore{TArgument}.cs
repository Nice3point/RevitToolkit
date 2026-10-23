using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Provides the queue of raises of an external event with an argument and the execution of each raise.
/// </summary>
/// <typeparam name="TArgument">The type of the argument passed to the handler.</typeparam>
/// <param name="handler">The execution logic of the external event.</param>
/// <param name="raise">The function that raises the Revit external event.</param>
/// <remarks>
///     Revit runs a pending external event once however often it is raised.
///     Each execution runs the oldest raise and raises the event again for the rest, and a refused raise runs the rest in the same execution.<br />
///     The execution and the refusal of a raise compete for its reservation, and a raise that Revit refuses never executes.
/// </remarks>
internal sealed class ExternalEventCore<TArgument>(Action<UIApplication, TArgument> handler, Func<ExternalEventRequest> raise)
{
    private readonly ConcurrentQueue<Operation> _operations = new();

    /// <summary>
    ///     Executes the handler on the calling thread.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication" /> instance.</param>
    /// <param name="argument">The argument to pass to the handler.</param>
    public void Invoke(UIApplication uiApplication, TArgument argument)
    {
        handler(uiApplication, argument);
    }

    /// <summary>
    ///     Adds a raise to the end of the queue and raises the Revit external event.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>The result of the request to raise the Revit external event.</returns>
    /// <remarks>A raise that Revit refuses leaves the queue unexecuted.</remarks>
    public ExternalEventRequest Raise(TArgument argument)
    {
        var operation = new Operation(argument);
        _operations.Enqueue(operation);

        var request = raise();
        if (request.IsRefused)
        {
            operation.TryReserveExecution();
        }

        return request;
    }

    /// <summary>
    ///     Executes the oldest raise in the queue and raises the Revit external event again for the rest.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication" /> instance.</param>
    /// <remarks>An exception of the handler propagates to the caller.</remarks>
    public void Execute(UIApplication uiApplication)
    {
        while (TryDequeueAndReserveExecution(out var operation))
        {
            var executesNextRaise = !_operations.IsEmpty && raise().IsRefused;
            handler(uiApplication, operation.Argument);

            if (!executesNextRaise)
            {
                return;
            }
        }
    }

    private bool TryDequeueAndReserveExecution([NotNullWhen(true)] out Operation? operation)
    {
        while (_operations.TryDequeue(out operation))
        {
            if (operation.TryReserveExecution())
            {
                return true;
            }
        }

        return false;
    }

    private sealed class Operation(TArgument argument)
    {
        private int _executionReserved;

        public TArgument Argument { get; } = argument;

        public bool TryReserveExecution()
        {
            return Interlocked.Exchange(ref _executionReserved, 1) == 0;
        }
    }
}
