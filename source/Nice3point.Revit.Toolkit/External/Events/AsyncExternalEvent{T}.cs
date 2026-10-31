using System.ComponentModel;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic asynchronous external event expanding <see cref="AsyncExternalEvent" /> that accepts an argument of type <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">The type of the argument passed to the handler.</typeparam>
[PublicAPI]
public sealed class AsyncExternalEvent<T> : ExternalEventHandler, IAsyncExternalEvent<T>
{
    private readonly AsyncExternalEventCore<T, VoidResult> _core;
    private readonly ExternalEventOptions _options;
    private readonly Lock _argumentLock = new();
    private T _argument = default!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<T> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<T> handler, ExternalEventOptions options) : base(handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((_, argument, _) =>
        {
            handler(argument);
            return default;
        }, base.Raise);

        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, T> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, T> handler, ExternalEventOptions options) : base(handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((uiApplication, argument, _) =>
        {
            handler(uiApplication, argument);
            return default;
        }, base.Raise);

        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" />, an argument of type <typeparamref name="T" />, and the token passed to <see cref="RaiseAsync(T, CancellationToken)" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, T, CancellationToken> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" />, an argument of type <typeparamref name="T" />, and the token passed to <see cref="RaiseAsync(T, CancellationToken)" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, T, CancellationToken> handler, ExternalEventOptions options) : base(handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((uiApplication, argument, cancellationToken) =>
        {
            handler(uiApplication, argument, cancellationToken);
            return default;
        }, base.Raise);

        _options = options;
    }

    /// <summary>
    ///     Raises the external event asynchronously with the specified argument, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit, which is the same policy like the one that applies to evoking external commands.<br />
    ///     Each call executes the handler once with its own argument, in the order of the calls.
    ///     The returned task completes with the outcome of that execution.
    ///     A call that Revit refuses with <see cref="ExternalEventRequest.Denied" /> or <see cref="ExternalEventRequest.TimedOut" /> fails its task with an <see cref="System.InvalidOperationException" /> without executing the handler.<br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode, the handler is executed directly on the calling thread instead of being queued.
    ///     This overload passes <see cref="CancellationToken.None" /> to <see cref="RaiseAsync(T, CancellationToken)" />.
    /// </remarks>
    public Task RaiseAsync(T argument)
    {
        return RaiseAsync(argument, CancellationToken.None);
    }

    /// <summary>
    ///     Raises the external event asynchronously with the specified argument, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit, which is the same policy like the one that applies to evoking external commands.<br />
    ///     Each call executes the handler once with its own argument, in the order of the calls.
    ///     The returned task completes with the outcome of that execution.
    ///     A call that Revit refuses with <see cref="ExternalEventRequest.Denied" /> or <see cref="ExternalEventRequest.TimedOut" /> fails its task with an <see cref="System.InvalidOperationException" /> without executing the handler.<br />
    ///     A cancellation requested before Revit executes the handler cancels the task at once without executing the handler.
    ///     A cancellation requested while the handler executes doesn't interrupt it.
    ///     An <see cref="System.OperationCanceledException" /> that the handler throws for the token cancels the task.<br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode, the handler is executed directly on the calling thread instead of being queued.
    ///     The token is checked once, before the handler executes.
    /// </remarks>
    public Task RaiseAsync(T argument, CancellationToken cancellationToken)
    {
        lock (_argumentLock)
        {
            _argument = argument;
        }

        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            return _core.Invoke(RevitContext.UiApplication, argument, cancellationToken);
        }

        return _core.RaiseAsync(argument, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Each call queues a separate execution with the last argument passed to <see cref="RaiseAsync(T, CancellationToken)" />, or the default value of <typeparamref name="T" /> before the first call.
    ///     The handler receives <see cref="CancellationToken.None" />.
    ///     Revit catches an exception of the handler.
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
