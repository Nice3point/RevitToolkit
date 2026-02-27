using System.ComponentModel;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Handler to provide access to modify the Revit document with the ability to queue calls.
/// </summary>
[PublicAPI]
public class ExternalEvent : ExternalEventHandler, IExternalEvent
{
    private readonly Action? _handler;
    private readonly Action<UIApplication>? _uiHandler;

    /// <summary>
    ///     Handler to provide access to modify the Revit document with the ability to queue calls.
    /// </summary>
    public ExternalEvent(Action handler)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Handler to provide access to modify the Revit document with the ability to queue calls.
    /// </summary>
    public ExternalEvent(Action<UIApplication> handler)
    {
        _uiHandler = handler;
    }

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        if (_uiHandler is not null)
        {
            _uiHandler.Invoke(uiApplication);
        }
        else
        {
            _handler!.Invoke();
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
    public void Raise(ExternalEventOptions options)
    {
        if ((options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            Execute(RevitContext.UiApplication);
            return;
        }

        base.Raise();
    }
}