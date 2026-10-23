// ReSharper disable once CheckNamespace

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic interface for raising an asynchronous external event
///     that returns a result of type <typeparamref name="TResult" />.
/// </summary>
/// <typeparam name="TResult">The type of the result produced by the async operation.</typeparam>
[PublicAPI]
public interface IAsyncRequestExternalEvent<TResult>
{
    /// <summary>
    ///     Raises (signals) the external event, instructing Revit to execute the handler within the Revit API context and wait for completion asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="Task{TResult}" /> representing the async operation being executed, containing the result of type <typeparamref name="TResult" />.</returns>
    /// <remarks>
    ///     A cancellation requested before Revit executes the handler withdraws the raise, and the task is canceled at once.
    ///     A cancellation requested while the handler executes doesn't interrupt it.
    /// </remarks>
    Task<TResult> RaiseAsync(CancellationToken cancellationToken = default);
}
