using System.ComponentModel;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic asynchronous external event expanding <see cref="AsyncExternalEvent" />
///     to return a result of type <typeparamref name="TResult" /> via <see cref="RaiseAsync" />.
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
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and the token passed to <see cref="RaiseAsync" />, returning a result of type <typeparamref name="TResult" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, CancellationToken, TResult> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{TResult}" /> class
    ///     with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and the token passed to <see cref="RaiseAsync" />, returning a result of type <typeparamref name="TResult" />.</param>
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
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="Task{TResult}" /> representing the async operation being executed, containing the result of type <typeparamref name="TResult" />.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br /><br />
    ///     Each call executes the handler once, in the order of the calls, and the returned task completes with the outcome of that execution.
    ///     A call that Revit refuses with <see cref="ExternalEventRequest.Denied" /> or <see cref="ExternalEventRequest.TimedOut" /> never executes the handler, and the task fails with an <see cref="System.InvalidOperationException" />.<br /><br />
    ///     A cancellation requested before Revit executes the handler withdraws the call: the handler doesn't execute, and the task is canceled at once.
    ///     A cancellation requested while the handler executes doesn't interrupt it, and the task completes with its result.
    ///     A handler that receives the token can throw an <see cref="System.OperationCanceledException" /> for it, and the task is then canceled.<br /><br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued, and the token is checked once before the handler executes.
    /// </remarks>
    public Task<TResult> RaiseAsync(CancellationToken cancellationToken = default)
    {
        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            return _core.Invoke(RevitContext.UiApplication, default, cancellationToken);
        }

        return _core.RaiseAsync(default, cancellationToken);
    }

    /// <summary>Callback invoked by Revit. Not intended to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        _core.Execute(uiApplication);
    }
}
