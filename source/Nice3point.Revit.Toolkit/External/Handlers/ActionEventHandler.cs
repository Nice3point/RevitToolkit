using System.Collections.Concurrent;
using System.ComponentModel;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Handler to provide access to modify the Revit document with the ability to queue calls.
/// </summary>
[PublicAPI]
public class ActionEventHandler : ExternalEventHandler
{
    private readonly ConcurrentQueue<Action<UIApplication>> _queue = new();
    private Action<Exception>? _exceptionHandler;

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
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
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then
    ///     it will execute its event handler.
    ///     Revit processes external events only when no other commands or
    ///     edit modes are currently active in Revit, which is the same policy
    ///     like the one that applies to evoking external commands.<br/><br/>
    ///     Executes the handler out of queue if Revit is in API mode.
    /// </remarks>
    public void Raise(Action<UIApplication> action)
    {
        if (RevitContext.IsRevitInApiMode)
        {
            action(RevitContext.UiApplication);
            return;
        }

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