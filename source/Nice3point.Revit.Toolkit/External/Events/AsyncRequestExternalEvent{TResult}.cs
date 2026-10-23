using System.ComponentModel;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic asynchronous external event expanding <see cref="AsyncExternalEvent" />
///     to return a result of type <typeparamref name="TResult" /> via <see cref="RaiseAsync()" />.
/// </summary>
/// <typeparam name="TResult">The type of the result produced by the handler.</typeparam>
[PublicAPI]
public sealed class AsyncRequestExternalEvent<TResult> : ExternalEventHandler, IAsyncRequestExternalEvent<TResult>
{
    private readonly AsyncExternalEventCore<VoidResult, TResult> _core;
    private readonly ExternalEventOptions _options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{TResult}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that returns a result of type <typeparamref name="TResult" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<TResult> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{TResult}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that returns a result of type <typeparamref name="TResult" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<TResult> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((_, _, _) => handler(), base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{TResult}" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and returns a result of type <typeparamref name="TResult" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, TResult> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{TResult}" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and returns a result of type <typeparamref name="TResult" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, TResult> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((uiApplication, _, _) => handler(uiApplication), base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{TResult}" /> class
    ///     with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and the token passed to <see cref="RaiseAsync(CancellationToken)" />, returning a result of type <typeparamref name="TResult" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, CancellationToken, TResult> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{TResult}" /> class
    ///     with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and the token passed to <see cref="RaiseAsync(CancellationToken)" />, returning a result of type <typeparamref name="TResult" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, CancellationToken, TResult> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((uiApplication, _, cancellationToken) => handler(uiApplication, cancellationToken), base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Raises the external event asynchronously, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <returns>The <see cref="Task{TResult}" /> representing the async operation being executed, containing the result of type <typeparamref name="TResult" />.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br /><br />
    ///     Each call executes the handler once, in the order of the calls.
    ///     The returned task completes with the outcome of that execution.
    ///     A call that Revit refuses with <see cref="ExternalEventRequest.Denied" /> or <see cref="ExternalEventRequest.TimedOut" /> fails its task with an <see cref="System.InvalidOperationException" /> without executing the handler.<br /><br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued.
    ///     This overload passes <see cref="CancellationToken.None" /> to <see cref="RaiseAsync(CancellationToken)" />.
    /// </remarks>
    public Task<TResult> RaiseAsync()
    {
        return RaiseAsync(CancellationToken.None);
    }

    /// <summary>
    ///     Raises the external event asynchronously, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="Task{TResult}" /> representing the async operation being executed, containing the result of type <typeparamref name="TResult" />.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br /><br />
    ///     Each call executes the handler once, in the order of the calls.
    ///     The returned task completes with the outcome of that execution.
    ///     A call that Revit refuses with <see cref="ExternalEventRequest.Denied" /> or <see cref="ExternalEventRequest.TimedOut" /> fails its task with an <see cref="System.InvalidOperationException" /> without executing the handler.<br /><br />
    ///     A cancellation requested before Revit executes the handler cancels the task at once without executing the handler.
    ///     A cancellation requested while the handler executes doesn't interrupt it.
    ///     An <see cref="System.OperationCanceledException" /> that the handler throws for the token cancels the task.<br /><br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued.
    ///     The token is checked once, before the handler executes.
    /// </remarks>
    public Task<TResult> RaiseAsync(CancellationToken cancellationToken)
    {
        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            return _core.Invoke(RevitContext.UiApplication, default, cancellationToken);
        }

        return _core.RaiseAsync(default, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Each call queues a separate execution without waiting for completion.
    ///     The handler receives <see cref="CancellationToken.None" />; its result is discarded and an exception propagates to Revit.
    /// </remarks>
    public override ExternalEventRequest Raise()
    {
        return _core.Raise(default);
    }

    /// <summary>Callback invoked by Revit. Not intended to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        _core.Execute(uiApplication);
    }
}
