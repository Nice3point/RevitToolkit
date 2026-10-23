using System.ComponentModel;
using Autodesk.Revit.UI;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic asynchronous external event expanding <see cref="AsyncExternalEvent" />
///     that accepts an argument of type <typeparamref name="T" />
///     and returns a result of type <typeparamref name="TResult" /> via <see cref="RaiseAsync(T)" />.
/// </summary>
/// <typeparam name="T">The type of the argument passed to the handler.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the handler.</typeparam>
[PublicAPI]
public sealed class AsyncRequestExternalEvent<T, TResult> : ExternalEventHandler, IAsyncRequestExternalEvent<T, TResult>
{
    private readonly AsyncExternalEventCore<T, TResult> _core;
    private readonly ExternalEventOptions _options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T" /> and returns a result of type <typeparamref name="TResult" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<T, TResult> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T" /> and returns a result of type <typeparamref name="TResult" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<T, TResult> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((_, argument, _) => handler(argument), base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />, returning a result of type <typeparamref name="TResult" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, T, TResult> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />, returning a result of type <typeparamref name="TResult" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, T, TResult> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new((uiApplication, argument, _) => handler(uiApplication, argument), base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}" /> class
    ///     with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" />, an argument of type <typeparamref name="T" />, and the token passed to <see cref="RaiseAsync(T, CancellationToken)" />, returning a result of type <typeparamref name="TResult" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, T, CancellationToken, TResult> handler) : this(handler, ExternalEventOptions.None)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}" /> class
    ///     with access to the <see cref="UIApplication" /> instance and the cancellation token of the raise.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" />, an argument of type <typeparamref name="T" />, and the token passed to <see cref="RaiseAsync(T, CancellationToken)" />, returning a result of type <typeparamref name="TResult" />.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, T, CancellationToken, TResult> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _core = new(handler, base.Raise);
        _options = options;
    }

    /// <summary>
    ///     Raises the external event asynchronously with the specified argument,
    ///     instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>The <see cref="Task{TResult}" /> representing the async operation being executed, containing the result of type <typeparamref name="TResult" />.</returns>
    /// <remarks>This overload passes <see cref="CancellationToken.None" /> to <see cref="RaiseAsync(T, CancellationToken)" />.</remarks>
    public Task<TResult> RaiseAsync(T argument)
    {
        return RaiseAsync(argument, CancellationToken.None);
    }

    /// <summary>
    ///     Raises the external event asynchronously with the specified argument,
    ///     instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="Task{TResult}" /> representing the async operation being executed, containing the result of type <typeparamref name="TResult" />.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br /><br />
    ///     Each call executes the handler once with its own argument, in the order of the calls, and the returned task completes with the outcome of that execution.
    ///     A call that Revit refuses with <see cref="ExternalEventRequest.Denied" /> or <see cref="ExternalEventRequest.TimedOut" /> never executes the handler, and the task fails with an <see cref="System.InvalidOperationException" />.<br /><br />
    ///     A cancellation requested before Revit executes the handler withdraws the call: the handler doesn't execute, and the task is canceled at once.
    ///     A cancellation requested while the handler executes doesn't interrupt it, and the task completes with its result.
    ///     A handler that receives the token can throw an <see cref="System.OperationCanceledException" /> for it, and the task is then canceled.<br /><br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued, and the token is checked once before the handler executes.
    /// </remarks>
    public Task<TResult> RaiseAsync(T argument, CancellationToken cancellationToken)
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
