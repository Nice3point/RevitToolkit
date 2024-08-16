using System.ComponentModel;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.External.Handlers;

/// <summary>
///     Handler, to provide access to change the Revit document with the ability to queue calls to Raise methods
/// </summary>
[PublicAPI]
public class ActionEventHandler : ExternalEventHandler
{
    private Action<UIApplication>? _action;

    /// <summary>Callback invoked by Revit. Not used to be called in user code</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        if (_action is null) return;

        try
        {
            _action(uiApplication);
        }
        finally
        {
            _action = null;
        }
    }

    /// <summary>
    ///     Instructing Revit to queue a handler and raise (signal) the external event
    /// </summary>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then
    ///     it will execute its event handler by calling the Execute method.
    ///     Revit processes external events only when no other commands or
    ///     edit modes are currently active in Revit, which is the same policy
    ///     like the one that applies to evoking external commands
    /// </remarks>
    public void Raise(Action<UIApplication> action)
    {
        if (_action is null) _action = action;
        else _action += action;

        Raise();
    }

    /// <summary>
    ///     Clears the call queue of subscribed delegates
    /// </summary>
    /// <remarks>The queue can be cleaned up before the first delegate is invoked</remarks>
    public void Cancel()
    {
        _action = null;
    }
}