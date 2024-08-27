using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     External event used to change the document.
/// </summary>
[PublicAPI]
public abstract class ExternalEventHandler : IExternalEventHandler
{
    private string? _identifier;
    private readonly ExternalEvent _externalEvent;

    /// <summary>
    ///     Creates an instance of external event.
    /// </summary>
    protected ExternalEventHandler()
    {
        _externalEvent = ExternalEvent.Create(this);
    }

    /// <summary>
    ///     This method is called to handle the external event.
    /// </summary>
    public abstract void Execute(UIApplication uiApplication);

    /// <summary>
    ///     String identification of the event handler.
    /// </summary>
    /// <returns>Event name</returns>
    public string GetName()
    {
        return _identifier ??= GetType().Name;
    }

    /// <summary>
    ///     Instructing Revit to raise (signal) the external event.
    /// </summary>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then
    ///     it will execute its event handler by calling the Execute method.
    ///     Revit processes external events only when no other commands or
    ///     edit modes are currently active in Revit, which is the same policy
    ///     like the one that applies to evoking external commands.
    /// </remarks>
    /// <returns>
    ///     The result of event raising request. If the request is 'Accepted',
    ///     the event would be added to the event queue and its handler will
    ///     be executed in the next event-processing cycle.
    /// </returns>
    public void Raise()
    {
        _externalEvent.Raise();
    }
}