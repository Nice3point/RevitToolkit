using System.ComponentModel;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     An asynchronous external event expanding <see cref="Nice3point.Revit.Toolkit.External.ExternalEvent" />
///     with the ability to await completion of the handler execution.
/// </summary>
[PublicAPI]
public sealed class AsyncExternalEvent : ExternalEventHandler, IAsyncExternalEvent
{
    private readonly AsyncExternalEventCore<VoidResult, VoidResult> _core;
    private readonly ExternalEventOptions _options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent" /> class.
    /// </summary>
    /// <param name="handler">The execution logic.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent" /> class.
    /// </summary>
    /// <param name="handler">The execution logic.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((_, _, _) =>
        {
            handler();
            return default;
        }, base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((uiApplication, _, _) =>
        {
            handler(uiApplication);
            return default;
        }, base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent" /> class
    ///     with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and the token passed to <see cref="RaiseAsync(CancellationToken)" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, CancellationToken> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent" /> class
    ///     with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and the token passed to <see cref="RaiseAsync(CancellationToken)" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, CancellationToken> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((uiApplication, _, cancellationToken) =>
        {
            handler(uiApplication, cancellationToken);
            return default;
        }, base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Raises the external event asynchronously, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    /// <remarks>This overload passes <see cref="CancellationToken.None" /> to <see cref="RaiseAsync(CancellationToken)" />.</remarks>
    public Task RaiseAsync()
    {
        return RaiseAsync(CancellationToken.None);
    }

    /// <summary>
    ///     Raises the external event asynchronously, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
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
    public Task RaiseAsync(CancellationToken cancellationToken)
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
