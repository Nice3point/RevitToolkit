using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     An interface for raising an external event with an argument of type <typeparamref name="T" />
///     to execute a handler within the Revit API context.
/// </summary>
/// <typeparam name="T">The type of the argument passed to the handler.</typeparam>
[PublicAPI]
public interface IExternalEvent<in T>
{
    /// <summary>
    ///     Raises (signals) the external event with the specified argument,
    ///     instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>
    ///     The result of event raising request. If the request is 'Accepted',
    ///     the event would be added to the event queue and its handler will
    ///     be executed in the next event-processing cycle.
    /// </returns>
    ExternalEventRequest Raise(T argument);
}
