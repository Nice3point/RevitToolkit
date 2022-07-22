using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.ExternalHandling.Handlers;

/// <summary>
///     Handler, to provide access to change the Revit document from modeless windows
/// </summary>
public class ActionEventHandler : ExternalEventHandler
{
    private Action<UIApplication> _action;

    public override void Execute(UIApplication uiApplication)
    {
        _action?.Invoke(uiApplication);
    }

    /// <summary>
    ///     Instructing Revit to raise (signal) the external event
    /// </summary>
    /// <remarks>
    ///    Revit will wait until it is ready to process the event and then
    ///    it will execute its event handler by calling the Execute method.
    ///    Revit processes external events only when no other commands or
    ///    edit modes are currently active in Revit, which is the same policy
    ///    like the one that applies to evoking external commands
    /// </remarks>
    /// <returns>
    ///    The result of event raising request. If the request is 'Accepted',
    ///    the event would be added to the event queue and its handler will
    ///    be executed in the next event-processing cycle
    /// </returns>
    public void Raise(Action<UIApplication> action)
    {
        _action = action;
        Raise();
    }
}

/// <summary>
///     Handler, to provide access to change the Revit document from modeless windows
/// </summary>
public class ActionEventHandler<T> : ExternalEventHandler
{
    private T _param;
    private Action<UIApplication, T> _action;

    public override void Execute(UIApplication uiApplication)
    {
        _action?.Invoke(uiApplication, _param);
    }

    /// <summary>
    ///     Instructing Revit to raise (signal) the external event
    /// </summary>
    /// <remarks>
    ///    Revit will wait until it is ready to process the event and then
    ///    it will execute its event handler by calling the Execute method.
    ///    Revit processes external events only when no other commands or
    ///    edit modes are currently active in Revit, which is the same policy
    ///    like the one that applies to evoking external commands
    /// </remarks>
    /// <returns>
    ///    The result of event raising request. If the request is 'Accepted',
    ///    the event would be added to the event queue and its handler will
    ///    be executed in the next event-processing cycle
    /// </returns>
    public void Raise(T param, Action<UIApplication, T> action)
    {
        _param = param;
        _action = action;
        Raise();
    }
}

/// <summary>
///     Handler, to provide access to change the Revit document from modeless windows
/// </summary>
public class ActionEventHandler<T0, T1> : ExternalEventHandler
{
    private T0 _param0;
    private T1 _param1;
    private Action<UIApplication, T0, T1> _action;

    public override void Execute(UIApplication uiApplication)
    {
        _action?.Invoke(uiApplication, _param0, _param1);
    }

    /// <summary>
    ///     Instructing Revit to raise (signal) the external event
    /// </summary>
    /// <remarks>
    ///    Revit will wait until it is ready to process the event and then
    ///    it will execute its event handler by calling the Execute method.
    ///    Revit processes external events only when no other commands or
    ///    edit modes are currently active in Revit, which is the same policy
    ///    like the one that applies to evoking external commands
    /// </remarks>
    /// <returns>
    ///    The result of event raising request. If the request is 'Accepted',
    ///    the event would be added to the event queue and its handler will
    ///    be executed in the next event-processing cycle
    /// </returns>
    public void Raise(T0 param0, T1 param1, Action<UIApplication, T0, T1> action)
    {
        _param0 = param0;
        _param1 = param1;
        _action = action;
        Raise();
    }
}

/// <summary>
///     Handler, to provide access to change the Revit document from modeless windows
/// </summary>
public class ActionEventHandler<T0, T1, T2> : ExternalEventHandler
{
    private T0 _param0;
    private T1 _param1;
    private T2 _param2;
    private Action<UIApplication, T0, T1, T2> _action;

    public override void Execute(UIApplication uiApplication)
    {
        _action?.Invoke(uiApplication, _param0, _param1, _param2);
    }

    /// <summary>
    ///     Instructing Revit to raise (signal) the external event
    /// </summary>
    /// <remarks>
    ///    Revit will wait until it is ready to process the event and then
    ///    it will execute its event handler by calling the Execute method.
    ///    Revit processes external events only when no other commands or
    ///    edit modes are currently active in Revit, which is the same policy
    ///    like the one that applies to evoking external commands
    /// </remarks>
    /// <returns>
    ///    The result of event raising request. If the request is 'Accepted',
    ///    the event would be added to the event queue and its handler will
    ///    be executed in the next event-processing cycle
    /// </returns>
    public void Raise(T0 param0, T1 param1, T2 param2, Action<UIApplication, T0, T1, T2> action)
    {
        _param0 = param0;
        _param1 = param1;
        _param2 = param2;
        _action = action;
        Raise();
    }
}