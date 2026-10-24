// ReSharper disable once CheckNamespace

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     An interface expanding <see cref="IExternalEvent" /> to support asynchronous operations.
/// </summary>
[PublicAPI]
public interface IAsyncExternalEvent
{
    /// <summary>
    ///     Raises (signals) the external event, instructing Revit to execute the handler within the Revit API context and wait for completion asynchronously.
    /// </summary>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    Task RaiseAsync();

    /// <summary>
    ///     Raises (signals) the external event, instructing Revit to execute the handler within the Revit API context and wait for completion asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    /// <remarks>
    ///     A cancellation requested before Revit executes the handler cancels the task at once without executing the handler.
    ///     A cancellation requested while the handler executes doesn't interrupt it.
    /// </remarks>
    Task RaiseAsync(CancellationToken cancellationToken);
}
