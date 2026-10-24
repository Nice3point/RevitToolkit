using System.ComponentModel;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic synchronous external event expanding <see cref="ExternalEvent" /> that accepts an argument of type <typeparamref name="T" /> and relays its functionality by invoking delegates within the Revit API context.
/// </summary>
/// <typeparam name="T">The type of the argument passed to the handler.</typeparam>
[PublicAPI]
public class ExternalEvent<T> : ExternalEventHandler, IExternalEvent<T>
{
    private readonly ExternalEventCore<T> _core;
    private readonly ExternalEventOptions _options;
    private readonly Lock _argumentLock = new();
    private T _argument = default!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent{T}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public ExternalEvent(Action<T> handler) : this(handler, ExternalEventOptions.None)
    {
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

        _core = new((_, argument) => handler(argument), base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent{T}" /> class with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public ExternalEvent(Action<UIApplication, T> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent{T}" /> class with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public ExternalEvent(Action<UIApplication, T> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new(handler, base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Raises (signals) the external event with the specified argument, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>
    ///     The result of event raising request.
    ///     If the request is 'Accepted', the event would be added to the event queue and its handler will be executed in the next event-processing cycle.
    /// </returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit, which is the same policy like the one that applies to evoking external commands.<br />
    ///     Each call executes the handler once with its own argument, in the order of the calls.
    ///     A call that returns <see cref="ExternalEventRequest.Denied" /> or <see cref="ExternalEventRequest.TimedOut" /> never executes the handler.<br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode, the handler is executed directly on the calling thread instead of being queued.
    /// </remarks>
    public ExternalEventRequest Raise(T argument)
    {
        lock (_argumentLock)
        {
            _argument = argument;
        }

        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            _core.Invoke(RevitContext.UiApplication, argument);
            return ExternalEventRequest.Accepted;
        }

        return _core.Raise(argument);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Each call queues a separate execution with the last argument passed to <see cref="Raise(T)" />, or the default value of <typeparamref name="T" /> before the first call.
    /// </remarks>
    public override ExternalEventRequest Raise()
    {
        T argument;
        lock (_argumentLock)
        {
            argument = _argument;
        }

        return _core.Raise(argument);
    }

    /// <summary>Callback invoked by Revit.</summary>
    /// <remarks>Not intended to be called in user code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        _core.Execute(uiApplication);
    }
}
