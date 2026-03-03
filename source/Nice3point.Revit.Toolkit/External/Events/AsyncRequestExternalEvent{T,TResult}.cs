using System.ComponentModel;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic asynchronous external event expanding <see cref="AsyncExternalEvent"/>
///     that accepts an argument of type <typeparamref name="T"/>
///     and returns a result of type <typeparamref name="TResult"/> via <see cref="RaiseAsync"/>.
/// </summary>
/// <typeparam name="T">The type of the argument passed to the handler.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the handler.</typeparam>
[PublicAPI]
public sealed class AsyncRequestExternalEvent<T, TResult> : ExternalEventHandler, IAsyncRequestExternalEvent<T, TResult>
{
    private readonly Func<T, TResult>? _handler;
    private readonly Func<UIApplication, T, TResult>? _uiHandler;
    private readonly ExternalEventOptions _options;
    private T _argument = default!;
    private TaskCompletionSource<TResult>? _taskCompletionSource;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}"/> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T"/> and returns a result of type <typeparamref name="TResult"/>.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public AsyncRequestExternalEvent(Func<T, TResult> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}"/> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T"/> and returns a result of type <typeparamref name="TResult"/>.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public AsyncRequestExternalEvent(Func<T, TResult> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}"/> class
    ///     with access to the <see cref="UIApplication"/> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication"/> and an argument of type <typeparamref name="T"/>, returning a result of type <typeparamref name="TResult"/>.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, T, TResult> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _uiHandler = handler;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRequestExternalEvent{T, TResult}"/> class
    ///     with access to the <see cref="UIApplication"/> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication"/> and an argument of type <typeparamref name="T"/>, returning a result of type <typeparamref name="TResult"/>.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public AsyncRequestExternalEvent(Func<UIApplication, T, TResult> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _uiHandler = handler;
        _options = options;
    }

    /// <summary>Callback invoked by Revit. Not intended to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        try
        {
            var result = ExecuteHandler(uiApplication);
            _taskCompletionSource?.SetResult(result);
        }
        catch (Exception exception)
        {
            _taskCompletionSource?.SetException(exception);
        }
    }

    /// <summary>
    ///     Raises the external event asynchronously with the specified argument,
    ///     instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>The <see cref="Task{TResult}"/> representing the async operation being executed, containing the result of type <typeparamref name="TResult"/>.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br/><br/>
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation"/> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued.
    /// </remarks>
    public Task<TResult> RaiseAsync(T argument)
    {
        _argument = argument;

        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            try
            {
                var result = ExecuteHandler(RevitContext.UiApplication);
                return Task.FromResult(result);
            }
            catch (Exception exception)
            {
                return Task.FromException<TResult>(exception);
            }
        }

        _taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Raise();

        return _taskCompletionSource.Task;
    }

    private TResult ExecuteHandler(UIApplication uiApplication)
    {
        TResult result;
        if (_uiHandler is not null)
        {
            result = _uiHandler.Invoke(uiApplication, _argument);
        }
        else
        {
            result = _handler!.Invoke(_argument);
        }

        return result;
    }
}
