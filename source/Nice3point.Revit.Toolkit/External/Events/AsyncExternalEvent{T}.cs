using System.ComponentModel;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic asynchronous external event expanding <see cref="AsyncExternalEvent" />
///     that accepts an argument of type <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">The type of the argument passed to the handler.</typeparam>
[PublicAPI]
public sealed class AsyncExternalEvent<T> : ExternalEventHandler, IAsyncExternalEvent<T>
{
    private readonly AsyncExternalEventCore<T, VoidResult> _core;
    private readonly ExternalEventOptions _options;

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
    public AsyncExternalEvent(Action<T> handler, ExternalEventOptions options)
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
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, T> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, T> handler, ExternalEventOptions options)
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
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class
    ///     with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" />, an argument of type <typeparamref name="T" />, and the token passed to <see cref="RaiseAsync(T, CancellationToken)" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, T, CancellationToken> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class
    ///     with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" />, an argument of type <typeparamref name="T" />, and the token passed to <see cref="RaiseAsync(T, CancellationToken)" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, T, CancellationToken> handler, ExternalEventOptions options)
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
    ///     Raises the external event asynchronously with the specified argument,
    ///     instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    /// <remarks>This overload passes <see cref="CancellationToken.None" /> to <see cref="RaiseAsync(T, CancellationToken)" />.</remarks>
    public Task RaiseAsync(T argument)
    {
        return RaiseAsync(argument, CancellationToken.None);
    }

    /// <summary>
    ///     Raises the external event asynchronously with the specified argument,
    ///     instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br /><br />
    ///     Each call executes the handler once with its own argument, in the order of the calls, and the returned task completes with the outcome of that execution.
    ///     A call that Revit refuses with <see cref="ExternalEventRequest.Denied" /> or <see cref="ExternalEventRequest.TimedOut" /> never executes the handler, and the task fails with an <see cref="System.InvalidOperationException" />.<br /><br />
    ///     A cancellation requested before Revit executes the handler withdraws the call: the handler doesn't execute, and the task is canceled at once.
    ///     A cancellation requested while the handler executes doesn't interrupt it.
    ///     A handler that receives the token can throw an <see cref="System.OperationCanceledException" /> for it, and the task is then canceled.<br /><br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued, and the token is checked once before the handler executes.
    /// </remarks>
    public Task RaiseAsync(T argument, CancellationToken cancellationToken)
    {
        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            return _core.Invoke(RevitContext.UiApplication, argument, cancellationToken);
        }

        return _core.RaiseAsync(argument, cancellationToken);
    }

    /// <summary>Callback invoked by Revit. Not intended to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        _core.Execute(uiApplication);
    }
}
