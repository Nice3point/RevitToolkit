using System;
using System.Threading;
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
    public interface IAsyncExternalEvent { Task RaiseAsync(CancellationToken cancellationToken = default); }
    public interface IAsyncExternalEvent<in T> { Task RaiseAsync(T args, CancellationToken cancellationToken = default); }
    public interface IAsyncRequestExternalEvent<TResult> { Task<TResult> RaiseAsync(CancellationToken cancellationToken = default); }
    public interface IAsyncRequestExternalEvent<in T, TResult> { Task<TResult> RaiseAsync(T args, CancellationToken cancellationToken = default); }

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
        private readonly Action<CancellationToken> _handler;
        public AsyncExternalEvent(Action handler, ExternalEventOptions options = default)
            : this((application, cancellationToken) => handler(), options) { }
        public AsyncExternalEvent(Action<Autodesk.Revit.UI.UIApplication> handler, ExternalEventOptions options = default)
            : this((application, cancellationToken) => handler(application), options) { }
        public AsyncExternalEvent(Action<Autodesk.Revit.UI.UIApplication, CancellationToken> handler, ExternalEventOptions options = default)
        {
            _handler = cancellationToken => handler(new Autodesk.Revit.UI.UIApplication(), cancellationToken);
            EventConstruction.Observer?.Invoke();
        }
        public Task RaiseAsync(CancellationToken cancellationToken = default) { _handler(cancellationToken); return Task.CompletedTask; }
    }

    public class AsyncExternalEvent<T> : IAsyncExternalEvent<T>
    {
        private readonly Action<T, CancellationToken> _handler;
        public AsyncExternalEvent(Action<T> handler, ExternalEventOptions options = default)
            : this((application, args, cancellationToken) => handler(args), options) { }
        public AsyncExternalEvent(Action<Autodesk.Revit.UI.UIApplication, T> handler, ExternalEventOptions options = default)
            : this((application, args, cancellationToken) => handler(application, args), options) { }
        public AsyncExternalEvent(Action<Autodesk.Revit.UI.UIApplication, T, CancellationToken> handler, ExternalEventOptions options = default)
        {
            _handler = (args, cancellationToken) => handler(new Autodesk.Revit.UI.UIApplication(), args, cancellationToken);
            EventConstruction.Observer?.Invoke();
        }
        public Task RaiseAsync(T args, CancellationToken cancellationToken = default) { _handler(args, cancellationToken); return Task.CompletedTask; }
    }

    public class AsyncRequestExternalEvent<TResult> : IAsyncRequestExternalEvent<TResult>
    {
        private readonly Func<CancellationToken, TResult> _handler;
        public AsyncRequestExternalEvent(Func<TResult> handler, ExternalEventOptions options = default)
            : this((application, cancellationToken) => handler(), options) { }
        public AsyncRequestExternalEvent(Func<Autodesk.Revit.UI.UIApplication, TResult> handler, ExternalEventOptions options = default)
            : this((application, cancellationToken) => handler(application), options) { }
        public AsyncRequestExternalEvent(Func<Autodesk.Revit.UI.UIApplication, CancellationToken, TResult> handler, ExternalEventOptions options = default)
        {
            _handler = cancellationToken => handler(new Autodesk.Revit.UI.UIApplication(), cancellationToken);
            EventConstruction.Observer?.Invoke();
        }
        public Task<TResult> RaiseAsync(CancellationToken cancellationToken = default) => Task.FromResult(_handler(cancellationToken));
    }

    public class AsyncRequestExternalEvent<T, TResult> : IAsyncRequestExternalEvent<T, TResult>
    {
        private readonly Func<T, CancellationToken, TResult> _handler;
        public AsyncRequestExternalEvent(Func<T, TResult> handler, ExternalEventOptions options = default)
            : this((application, args, cancellationToken) => handler(args), options) { }
        public AsyncRequestExternalEvent(Func<Autodesk.Revit.UI.UIApplication, T, TResult> handler, ExternalEventOptions options = default)
            : this((application, args, cancellationToken) => handler(application, args), options) { }
        public AsyncRequestExternalEvent(Func<Autodesk.Revit.UI.UIApplication, T, CancellationToken, TResult> handler, ExternalEventOptions options = default)
        {
            _handler = (args, cancellationToken) => handler(new Autodesk.Revit.UI.UIApplication(), args, cancellationToken);
            EventConstruction.Observer?.Invoke();
        }
        public Task<TResult> RaiseAsync(T args, CancellationToken cancellationToken = default) => Task.FromResult(_handler(args, cancellationToken));
    }
}
