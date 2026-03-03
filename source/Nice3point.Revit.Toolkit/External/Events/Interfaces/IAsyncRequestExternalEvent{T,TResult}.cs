using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic interface for raising an asynchronous external event with an argument of type <typeparamref name="T"/>
///     that returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="T">The type of the argument passed to the handler.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the async operation.</typeparam>
[PublicAPI]
public interface IAsyncRequestExternalEvent<in T, TResult>
{
    /// <summary>
    ///     Raises (signals) the external event with the specified argument, instructing Revit to execute the handler within the Revit API context and wait for completion asynchronously.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>The <see cref="Task{TResult}"/> representing the async operation being executed, containing the result of type <typeparamref name="TResult"/>.</returns>
    Task<TResult> RaiseAsync(T argument);
}