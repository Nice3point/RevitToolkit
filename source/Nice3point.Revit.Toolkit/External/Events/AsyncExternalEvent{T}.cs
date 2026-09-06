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
    private readonly Action<T>? _handler;
    private readonly ExternalEventOptions _options;
    private readonly Action<UIApplication, T>? _uiHandler;
    private T _argument = default!;
    private TaskCompletionSource? _taskCompletionSource;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class.
    /// </summary>
    /// <param name="handler">The execution logic that receives an argument of type <typeparamref name="T" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
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

        _handler = handler;
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent{T}" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" /> and an argument of type <typeparamref name="T" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication, T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _uiHandler = handler;
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

        _uiHandler = handler;
        _options = options;
    }

    /// <summary>
    ///     Raises the external event asynchronously with the specified argument,
    ///     instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <param name="argument">The argument to pass to the handler.</param>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br /><br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued.
    /// </remarks>
    public Task RaiseAsync(T argument)
    {
        _argument = argument;

        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            try
            {
                ExecuteHandler(RevitContext.UiApplication);
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        _taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Raise();

        return _taskCompletionSource.Task;
    }

    /// <summary>Callback invoked by Revit. Not intended to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        try
        {
            ExecuteHandler(uiApplication);
            _taskCompletionSource?.SetResult();
        }
        catch (Exception exception)
        {
            _taskCompletionSource?.SetException(exception);
        }
    }

    private void ExecuteHandler(UIApplication uiApplication)
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
