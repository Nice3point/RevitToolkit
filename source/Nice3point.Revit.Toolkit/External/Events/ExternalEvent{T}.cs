using System.ComponentModel;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic synchronous external event expanding <see cref="ExternalEvent" />
///     that accepts an argument of type <typeparamref name="T" />
///     and relays its functionality by invoking delegates within the Revit API context.
/// </summary>
/// <typeparam name="T">The type of the argument passed to the handler.</typeparam>
[PublicAPI]
public class ExternalEvent<T> : ExternalEventHandler, IExternalEvent<T>
{
    private readonly Action<T>? _handler;
    private readonly ExternalEventOptions _options;
    private readonly Action<UIApplication, T>? _uiHandler;
    private T _argument = default!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent{T}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public ExternalEvent(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent{T}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public ExternalEvent(Action<T> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent{T}" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public ExternalEvent(Action<UIApplication, T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _uiHandler = handler;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent{T}" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public ExternalEvent(Action<UIApplication, T> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _uiHandler = handler;
        _options = options;
    }

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
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br /><br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued.
    /// </remarks>
    public ExternalEventRequest Raise(T argument)
    {
        _argument = argument;

        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            Execute(RevitContext.UiApplication);
            return ExternalEventRequest.Accepted;
        }

        return base.Raise();
    }

    /// <summary>Callback invoked by Revit. Not intended to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        if (_uiHandler is not null)
        {
            _uiHandler.Invoke(uiApplication, _argument);
        }
        else
        {
            _handler!.Invoke(_argument);
        }
    }
}
