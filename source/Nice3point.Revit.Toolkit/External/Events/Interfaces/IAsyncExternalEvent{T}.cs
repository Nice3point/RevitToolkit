// ReSharper disable once CheckNamespace

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic interface for raising an asynchronous external event with an argument of type <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">The type of the argument passed to the handler.</typeparam>
[PublicAPI]
public interface IAsyncExternalEvent<in T>
{
    /// <summary>
    ///     Raises (signals) the external event with the specified argument, instructing Revit to execute the handler within the Revit API context and wait for completion asynchronously.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    Task RaiseAsync(T argument);

    /// <summary>
    ///     Raises (signals) the external event with the specified argument, instructing Revit to execute the handler within the Revit API context and wait for completion asynchronously.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    /// <remarks>
    ///     A cancellation requested before Revit executes the handler withdraws the raise, and the task is canceled at once.
    ///     A cancellation requested while the handler executes doesn't interrupt it.
    /// </remarks>
    Task RaiseAsync(T argument, CancellationToken cancellationToken);
}
