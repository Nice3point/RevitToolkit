using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic interface representing a more specific version of <see cref="IAsyncExternalEvent"/>
///     that returns a result of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the result produced by the async operation.</typeparam>
[PublicAPI]
public interface IAsyncExternalEvent<T>
{
    /// <summary>
    ///     Raises the external event asynchronously, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <returns>The <see cref="Task{T}"/> representing the async operation being executed, containing the result of type <typeparamref name="T"/>.</returns>
    Task<T> RaiseAsync();
}
