using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.Internal;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A base class wrapping the Revit <see cref="Autodesk.Revit.UI.ExternalEvent" /> and <see cref="IExternalEventHandler" /> boilerplate to simplify external event handler implementation.
/// </summary>
/// <remarks>
///     The handler runs as the add-in that declares it, on any thread the event is created on.
///     <see cref="ExternalApplication" /> and <see cref="ExternalCommand" /> register their assembly with its project references automatically.
///     An assembly that creates an external event inside a Revit callback registers itself.
///     Other assemblies, for example a library from a NuGet package, are registered with <see cref="AddInAssemblyExtensions.RegisterAddInAssembly(UIControlledApplication, System.Reflection.Assembly)" />.
/// </remarks>
[PublicAPI]
public abstract class ExternalEventHandler : IExternalEventHandler
{
    private readonly Autodesk.Revit.UI.ExternalEvent _externalEvent;
    private string? _identifier;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEventHandler" /> class.
    /// </summary>
    /// <remarks>
    ///     The constructor creates the underlying Revit external event, bound to the add-in of the assembly of the derived type.
    /// </remarks>
    protected ExternalEventHandler() : this(handler: null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEventHandler" /> class.
    /// </summary>
    /// <param name="handler">The delegate that implements the execution logic, or <see langword="null" /> when the derived type implements it.</param>
    /// <remarks>
    ///     The constructor creates the underlying Revit external event, bound to the add-in of the handler assembly.
    /// </remarks>
    private protected ExternalEventHandler(Delegate? handler)
    {
        using (RevitContext.BeginApiContextScope())
        {
            _externalEvent = AddInContext.CreateExternalEvent(this, handler);
        }
    }

    /// <summary>
    ///     This method is called to handle the external event.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication" /> instance provided by Revit.</param>
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
    ///     by calling the <see cref="Execute" /> method. Revit processes external events only when no other commands
    ///     or edit modes are currently active in Revit, which is the same policy like the one that applies
    ///     to evoking external commands.
    /// </remarks>
    public virtual ExternalEventRequest Raise()
    {
        return _externalEvent.Raise();
    }
}
