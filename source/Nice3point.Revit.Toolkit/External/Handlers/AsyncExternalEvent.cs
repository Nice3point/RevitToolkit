using System.ComponentModel;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Handler to provide access to modify the Revit document asynchronously.
/// </summary>
[PublicAPI]
public sealed class AsyncExternalEvent : ExternalEventHandler, IAsyncExternalEvent
{
    private readonly Action? _handler;
    private readonly Action<UIApplication>? _uiHandler;
    private TaskCompletionSource? _taskCompletionSource;

    /// <summary>
    ///     Handler to provide access to modify the Revit document asynchronously.
    /// </summary>
    public AsyncExternalEvent(Action handler)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Handler to provide access to modify the Revit document asynchronously.
    /// </summary>
    public AsyncExternalEvent(Action<UIApplication> handler)
    {
        _uiHandler = handler;
    }

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
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

    /// <summary>
    ///     Instructing Revit to queue a handler, raise the external event and await completion.
    /// </summary>
    public Task RaiseAsync()
    {
        if (RevitContext.IsRevitInApiMode)
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
}