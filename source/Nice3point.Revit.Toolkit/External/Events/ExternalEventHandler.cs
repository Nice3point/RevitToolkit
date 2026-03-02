using Autodesk.Revit.UI;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A base class wrapping the Revit <see cref="Autodesk.Revit.UI.ExternalEvent"/> and
///     <see cref="IExternalEventHandler"/> boilerplate to simplify external event handler implementation.
/// </summary>
[PublicAPI]
public abstract class ExternalEventHandler : IExternalEventHandler
{
    private string? _identifier;
    private readonly Autodesk.Revit.UI.ExternalEvent _externalEvent;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEventHandler"/> class
    ///     and creates the underlying Revit external event.
    /// </summary>
    protected ExternalEventHandler()
    {
        using (RevitContext.BeginApiContextScope())
        {
            _externalEvent = Autodesk.Revit.UI.ExternalEvent.Create(this);
        }
    }

    /// <summary>
    ///     This method is called to handle the external event.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication"/> instance provided by Revit.</param>
    public abstract void Execute(UIApplication uiApplication);

    /// <summary>
    ///     String identification of the event handler.
    /// </summary>
    /// <returns>The name of the event handler type.</returns>
    public virtual string GetName()
    {
        return _identifier ??= GetType().Name;
    }

    /// <summary>
    ///     Raises (signals) the external event, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <returns>
    ///     The result of event raising request. If the request is 'Accepted',
    ///     the event would be added to the event queue and its handler will
    ///     be executed in the next event-processing cycle.
    /// </returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler
    ///     by calling the <see cref="Execute"/> method. Revit processes external events only when no other commands
    ///     or edit modes are currently active in Revit, which is the same policy like the one that applies
    ///     to evoking external commands.
    /// </remarks>
    public virtual ExternalEventRequest Raise()
    {
        return _externalEvent.Raise();
    }
}