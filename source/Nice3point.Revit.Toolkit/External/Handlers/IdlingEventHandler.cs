using System.Collections.Concurrent;
using System.ComponentModel;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using JetBrains.Annotations;

namespace Nice3point.Revit.Toolkit.External.Handlers;

/// <summary>
///     Handler to provide access to modify the Revit document when UIApplication.Idling occurs.
/// </summary>
[PublicAPI]
public class IdlingEventHandler : ExternalEventHandler
{
    private readonly ConcurrentQueue<Action<UIApplication>> _queue = new();
    private Action<Exception>? _exceptionHandler;

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        uiApplication.Idling += HandleIdling;
    }

    private void HandleIdling(object? sender, IdlingEventArgs e)
    {
        var uiApplication = (UIApplication)sender!;
        uiApplication.Idling -= HandleIdling;

        while (_queue.TryDequeue(out var action))
        {
            try
            {
                action(uiApplication);
            }
            catch (Exception exception)
            {
                _exceptionHandler?.Invoke(exception);
            }
        }
    }

    /// <summary>
    ///     Instructing Revit to queue a handler and raise (signal) the external event.
    /// </summary>
    public void Raise(Action<UIApplication> action)
    {
        _queue.Enqueue(action);
        Raise();
    }

    /// <summary>
    ///     Sets an optional exception handler to be called when an action throws an exception.
    /// </summary>
    /// <param name="handler">The exception handler callback.</param>
    /// <remarks>
    ///     Exceptions are caught and passed to this handler to ensure subsequent actions continue executing.
    ///     If no handler is set, exceptions are silently ignored.
    /// </remarks>
    public void SetExceptionHandler(Action<Exception> handler)
    {
        _exceptionHandler = handler;
    }
}