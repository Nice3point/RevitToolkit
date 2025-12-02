using System.Collections.Concurrent;
using System.ComponentModel;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Nice3point.Revit.Toolkit.External.Handlers;

/// <summary>
///     Handler to provide access to modify the Revit document when UIApplication.Idling occurs.
/// </summary>
[PublicAPI]
public class IdlingEventHandler : ExternalEventHandler
{
    private readonly ConcurrentQueue<Action<UIApplication>> _queue = new();

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
            catch
            {
                // Ignore exceptions to ensure subsequent actions are executed
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
}