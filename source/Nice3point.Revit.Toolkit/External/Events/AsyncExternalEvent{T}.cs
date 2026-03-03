using System.ComponentModel;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     A generic asynchronous external event expanding <see cref="AsyncExternalEvent"/>
///     to return a result of type <typeparamref name="T"/> via <see cref="RaiseAsync"/>.
/// </summary>
/// <typeparam name="T">The type of the result produced by the handler.</typeparam>
[PublicAPI]
public sealed class AsyncExternalEvent<T> : ExternalEventHandler, IAsyncExternalEvent<T>
{
    private readonly Func<T>? _handler;
    private readonly Func<UIApplication, T>? _uiHandler;
    private readonly ExternalEventOptions _options;
    private TaskCompletionSource<T>? _taskCompletionSource;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}"/> class.
    /// </summary>
    /// <param name="handler">The execution logic that returns a result of type <typeparamref name="T"/>.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public AsyncExternalEvent(Func<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}"/> class.
    /// </summary>
    /// <param name="handler">The execution logic that returns a result of type <typeparamref name="T"/>.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public AsyncExternalEvent(Func<T> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}"/> class
    ///     with access to the <see cref="UIApplication"/> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication"/> and returns a result of type <typeparamref name="T"/>.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public AsyncExternalEvent(Func<UIApplication, T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _uiHandler = handler;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}"/> class
    ///     with access to the <see cref="UIApplication"/> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication"/> and returns a result of type <typeparamref name="T"/>.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public AsyncExternalEvent(Func<UIApplication, T> handler, ExternalEventOptions options)
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
    ///     Raises the external event asynchronously, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <returns>The <see cref="Task{T}"/> representing the async operation being executed, containing the result of type <typeparamref name="T"/>.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br/><br/>
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation"/> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued.
    /// </remarks>
    public Task<T> RaiseAsync()
    {
        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            try
            {
                var result = ExecuteHandler(RevitContext.UiApplication);
                return Task.FromResult(result);
            }
            catch (Exception exception)
            {
                return Task.FromException<T>(exception);
            }
        }

        _taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Raise();

        return _taskCompletionSource.Task;
    }

    private T ExecuteHandler(UIApplication uiApplication)
    {
        T result;
        if (_uiHandler is not null)
        {
            result = _uiHandler.Invoke(uiApplication);
        }
        else
        {
            result = _handler!.Invoke();
        }

        return result;
    }
}