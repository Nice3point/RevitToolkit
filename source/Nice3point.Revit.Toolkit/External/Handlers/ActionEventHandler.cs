using System.Collections.Concurrent;
using System.ComponentModel;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.External.Handlers;

/// <summary>
///     Handler to provide access to modify the Revit document with the ability to queue calls.
/// </summary>
[PublicAPI]
public class ActionEventHandler : ExternalEventHandler
{
    private readonly ConcurrentQueue<Action<UIApplication>> _queue = new();

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
            catch
            {
                // Ignore exceptions to ensure subsequent actions are executed
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
        if (Context.IsRevitInApiMode)
        {
            action(Context.UiApplication);
            return;
        }

        _queue.Enqueue(action);
        Raise();
    }
}