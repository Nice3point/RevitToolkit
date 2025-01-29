using System.ComponentModel;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Nice3point.Revit.Toolkit.External.Handlers;

/// <summary>
///     Handler, to provide access to modify the Revit document with the ability to queue calls to Raise methods.<br />
///     Delegates will be invoked when UIApplication.Idling occurs
/// </summary>
/// <remarks>
///     Unsubscribing from the Idling event occurs immediately.<br />
///     Suitable for cases where you need to call code when Revit receives focus or to open a dialog after loading a family into the project.
/// </remarks>
[PublicAPI]
public class IdlingEventHandler : ExternalEventHandler
{
    private Action<UIApplication>? _handler;

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

        if (_handler is null) return;
        
        try
        {
            _handler(uiApplication);
        }
        finally
        {
            _handler = null;
        }
    }

    /// <summary>
    ///     Instructing Revit to queue a handler and raise (signal) the external event.
    /// </summary>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then
    ///     it will execute its event handler by calling the Execute method.
    ///     Revit processes external events only when no other commands or
    ///     edit modes are currently active in Revit, which is the same policy
    ///     like the one that applies to evoking external commands.
    /// </remarks>
    public void Raise(Action<UIApplication> action)
    {
        if (_handler is null) _handler = action;
        else _handler += action;

        Raise();
    }

    /// <summary>
    ///     Clears the call queue of subscribed delegates.
    /// </summary>
    /// <remarks>The queue can be cleaned up before the first delegate is invoked.</remarks>
    public void Cancel()
    {
        _handler = null;
    }
}