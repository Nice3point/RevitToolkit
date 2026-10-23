using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Contains extension members for <see cref="ExternalEventRequest" />.
/// </summary>
internal static class ExternalEventRequestExtensions
{
    /// <summary>
    ///     Defines extensions for <see cref="ExternalEventRequest" />.
    /// </summary>
    /// <param name="request">The result of a request to raise an external event.</param>
    extension(ExternalEventRequest request)
    {
        /// <summary>
        ///     Gets a value indicating whether Revit refused to raise the external event.
        /// </summary>
        public bool IsRefused => request is ExternalEventRequest.Denied or ExternalEventRequest.TimedOut;
    }
}
