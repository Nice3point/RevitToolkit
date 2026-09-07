using System;
using System.Threading.Tasks;

namespace Autodesk.Revit.UI
{
    public class UIApplication { }

    public enum ExternalEventRequest
    {
        Accepted,
        Denied,
        TimedOut
    }
}

namespace Nice3point.Revit.Toolkit.External
{
    public static class EventConstruction
    {
        public static Action? Observer { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExternalEventAttribute : Attribute
    {
        public bool AllowDirectInvocation { get; init; }
    }

    [Flags]
    public enum ExternalEventOptions { None = 0, AllowDirectInvocation = 1 }

    public interface IExternalEvent { Autodesk.Revit.UI.ExternalEventRequest Raise(); }
    public interface IExternalEvent<in T> { Autodesk.Revit.UI.ExternalEventRequest Raise(T args); }
    public interface IAsyncExternalEvent { Task RaiseAsync(); }
    public interface IAsyncExternalEvent<in T> { Task RaiseAsync(T args); }
    public interface IAsyncRequestExternalEvent<TResult> { Task<TResult> RaiseAsync(); }
    public interface IAsyncRequestExternalEvent<in T, TResult> { Task<TResult> RaiseAsync(T args); }

    public class ExternalEvent : IExternalEvent
    {
        private readonly Action _handler;
        public ExternalEvent(Action handler, ExternalEventOptions options = default) { _handler = handler; EventConstruction.Observer?.Invoke(); }
        public ExternalEvent(Action<Autodesk.Revit.UI.UIApplication> handler, ExternalEventOptions options = default)
            : this(() => handler(new Autodesk.Revit.UI.UIApplication()), options) { }
        public Autodesk.Revit.UI.ExternalEventRequest Raise() { _handler(); return default; }
    }

    public class ExternalEvent<T> : IExternalEvent<T>
    {
        private readonly Action<T> _handler;
        public ExternalEvent(Action<T> handler, ExternalEventOptions options = default) { _handler = handler; EventConstruction.Observer?.Invoke(); }
        public ExternalEvent(Action<Autodesk.Revit.UI.UIApplication, T> handler, ExternalEventOptions options = default)
            : this(args => handler(new Autodesk.Revit.UI.UIApplication(), args), options) { }
        public Autodesk.Revit.UI.ExternalEventRequest Raise(T args) { _handler(args); return default; }
    }

    public class AsyncExternalEvent : IAsyncExternalEvent
    {
        private readonly Action _handler;
        public AsyncExternalEvent(Action handler, ExternalEventOptions options = default) { _handler = handler; EventConstruction.Observer?.Invoke(); }
        public AsyncExternalEvent(Action<Autodesk.Revit.UI.UIApplication> handler, ExternalEventOptions options = default)
            : this(() => handler(new Autodesk.Revit.UI.UIApplication()), options) { }
        public Task RaiseAsync() { _handler(); return Task.CompletedTask; }
    }

    public class AsyncExternalEvent<T> : IAsyncExternalEvent<T>
    {
        private readonly Action<T> _handler;
        public AsyncExternalEvent(Action<T> handler, ExternalEventOptions options = default) { _handler = handler; EventConstruction.Observer?.Invoke(); }
        public AsyncExternalEvent(Action<Autodesk.Revit.UI.UIApplication, T> handler, ExternalEventOptions options = default)
            : this(args => handler(new Autodesk.Revit.UI.UIApplication(), args), options) { }
        public Task RaiseAsync(T args) { _handler(args); return Task.CompletedTask; }
    }

    public class AsyncRequestExternalEvent<TResult> : IAsyncRequestExternalEvent<TResult>
    {
        private readonly Func<TResult> _handler;
        public AsyncRequestExternalEvent(Func<TResult> handler, ExternalEventOptions options = default) { _handler = handler; EventConstruction.Observer?.Invoke(); }
        public AsyncRequestExternalEvent(Func<Autodesk.Revit.UI.UIApplication, TResult> handler, ExternalEventOptions options = default)
            : this(() => handler(new Autodesk.Revit.UI.UIApplication()), options) { }
        public Task<TResult> RaiseAsync() => Task.FromResult(_handler());
    }

    public class AsyncRequestExternalEvent<T, TResult> : IAsyncRequestExternalEvent<T, TResult>
    {
        private readonly Func<T, TResult> _handler;
        public AsyncRequestExternalEvent(Func<T, TResult> handler, ExternalEventOptions options = default) { _handler = handler; EventConstruction.Observer?.Invoke(); }
        public AsyncRequestExternalEvent(Func<Autodesk.Revit.UI.UIApplication, T, TResult> handler, ExternalEventOptions options = default)
            : this(args => handler(new Autodesk.Revit.UI.UIApplication(), args), options) { }
        public Task<TResult> RaiseAsync(T args) => Task.FromResult(_handler(args));
    }
}
