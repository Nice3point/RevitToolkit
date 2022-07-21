using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.ExternalHandling;

/// <summary>
///     External event used to change the model.
/// </summary>
/// <remarks>
///     In derived classes create an overload of the Raise() method to add parameters.
///     <code>
///          Command.EventHandler.Raise(value1, value2);
///      </code>
///     Or use public properties.
///     <code>
///         Command.EventHandler.Property1 = "value1";
///         Command.EventHandler.Property2 = "value2";
///         Command.EventHandler.Raise();
///      </code>
/// </remarks>
public abstract class ExternalEventHandler : IExternalEventHandler
{
    private readonly ExternalEvent _externalEvent;

    protected ExternalEventHandler()
    {
        _externalEvent = ExternalEvent.Create(this);
    }

    public virtual void Execute(UIApplication uiApplication)
    {
        throw new NotImplementedException();
    }

    public string GetName()
    {
        return GetType().Name;
    }

    /// <summary>
    ///     Instructing Revit to raise (signal) the external event
    /// </summary>
    /// <remarks>
    ///    Revit will wait until it is ready to process the event and then
    ///    it will execute its event handler by calling the Execute method.
    ///    Revit processes external events only when no other commands or
    ///    edit modes are currently active in Revit, which is the same policy
    ///    like the one that applies to evoking external commands
    /// </remarks>
    /// <returns>
    ///    The result of event raising request. If the request is 'Accepted',
    ///    the event would be added to the event queue and its handler will
    ///    be executed in the next event-processing cycle
    /// </returns>
    public void Raise()
    {
        _externalEvent.Raise();
    }
}