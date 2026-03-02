using System.ComponentModel;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     An external event whose sole purpose is to relay its functionality to other
///     objects by invoking delegates. The default behavior queues the handler
///     via the Revit external event mechanism. This type allows you to accept
///     a <see cref="UIApplication"/> parameter in the <see cref="Nice3point.Revit.Toolkit.External.ExternalEvent(Action{Autodesk.Revit.UI.UIApplication})"/> callback overload.
/// </summary>
[PublicAPI]
public class ExternalEvent : ExternalEventHandler, IExternalEvent
{
    private readonly Action? _handler;
    private readonly Action<UIApplication>? _uiHandler;
    private readonly ExternalEventOptions _options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Nice3point.Revit.Toolkit.External.ExternalEvent"/> class.
    /// </summary>
    /// <param name="handler">The execution logic.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public ExternalEvent(Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent"/> class.
    /// </summary>
    /// <param name="handler">The execution logic.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public ExternalEvent(Action handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
        _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent"/> class
    ///     with access to the <see cref="UIApplication"/> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication"/>.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public ExternalEvent(Action<UIApplication> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _uiHandler = handler;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExternalEvent"/> class
    ///     with access to the <see cref="UIApplication"/> instance.
    /// </summary>
    /// <param name="handler">The execution logic that receives the current <see cref="UIApplication"/>.</param>
    /// <param name="options">The options to use to configure the external event.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public ExternalEvent(Action<UIApplication> handler, ExternalEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _uiHandler = handler;
        _options = options;
    }

    /// <summary>Callback invoked by Revit. Not intended to be called in user code.</summary>
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
    ///     Raises (signals) the external event, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    /// <remarks>
    ///     Revit will wait until it is ready to process the event and then it will execute its event handler.
    ///     Revit processes external events only when no other commands or edit modes are currently active in Revit,
    ///     which is the same policy like the one that applies to evoking external commands.<br/><br/>
    ///     When <see cref="ExternalEventOptions.AllowDirectInvocation"/> is specified and Revit is in API mode,
    ///     the handler is executed directly on the calling thread instead of being queued.
    /// </remarks>
    public override void Raise()
    {
        if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
        {
            Execute(RevitContext.UiApplication);
            return;
        }

        base.Raise();
    }
}