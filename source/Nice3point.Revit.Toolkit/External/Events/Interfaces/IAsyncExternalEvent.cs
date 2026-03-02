using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     An interface expanding <see cref="IExternalEvent"/> to support asynchronous operations.
/// </summary>
[PublicAPI]
public interface IAsyncExternalEvent
{
    /// <summary>
    ///     Raises the external event asynchronously, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <returns>The <see cref="Task"/> representing the async operation being executed.</returns>
    Task RaiseAsync();
}
