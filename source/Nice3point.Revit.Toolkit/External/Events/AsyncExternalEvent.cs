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
    private readonly Action? _handler;
    private readonly ExternalEventOptions _options;
    private readonly Action<UIApplication>? _uiHandler;
    private TaskCompletionSource? _taskCompletionSource;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent" /> class.
    /// </summary>
    /// <param name="handler">The execution logic.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
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

        _handler = handler;
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncExternalEvent" /> class
    ///     with access to the <see cref="UIApplication" /> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler" /> is <see langword="null" />.</exception>
    public AsyncExternalEvent(Action<UIApplication> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _uiHandler = handler;
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

        _uiHandler = handler;
        _options = options;
    }

    /// <summary>
    ///     Raises the external event asynchronously, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <returns>The <see cref="Task" /> representing the async operation being executed.</returns>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br /><br />
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation" /> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued.
    /// </remarks>
    public Task RaiseAsync()
    {
        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            try
            {
                Execute(RevitContext.UiApplication);
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
            if (_uiHandler is not null)
            {
                _uiHandler.Invoke(uiApplication);
            }
            else
            {
                _handler!.Invoke();
            }

            _taskCompletionSource?.SetResult();
        }
        catch (Exception exception)
        {
            _taskCompletionSource?.SetException(exception);
        }
    }
}
