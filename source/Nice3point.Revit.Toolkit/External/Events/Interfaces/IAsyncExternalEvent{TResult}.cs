using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic interface representing a more specific version of <see cref="IAsyncExternalEvent"/>
///     that returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result produced by the async operation.</typeparam>
[PublicAPI]
public interface IAsyncExternalEvent<TResult>
{
    /// <summary>
    ///     Raises (signals) the external event, instructing Revit to execute the handler within the Revit API context and wait for completion asynchronously.
    /// </summary>
    /// <returns>The <see cref="Task{TResult}"/> representing the async operation being executed, containing the result of type <typeparamref name="T"/>.</returns>
    Task<TResult> RaiseAsync();
}