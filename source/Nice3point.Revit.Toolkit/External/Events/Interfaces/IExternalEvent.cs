using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     An interface for raising an external event to execute a handler within the Revit API context.
/// </summary>
[PublicAPI]
public interface IExternalEvent
{
    /// <summary>
    ///     Raises (signals) the external event, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <returns>
    ///     The result of event raising request. If the request is 'Accepted',
    ///     the event would be added to the event queue and its handler will
    ///     be executed in the next event-processing cycle.
    /// </returns>
    ExternalEventRequest Raise();
}
