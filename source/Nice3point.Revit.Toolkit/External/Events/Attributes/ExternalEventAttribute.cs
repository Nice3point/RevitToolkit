// ReSharper disable once CheckNamespace

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     An attribute that can be used to automatically generate <see cref="IExternalEvent" /> properties from declared methods.
///     When this attribute is used to decorate a method, a generator will create an external event property with the corresponding
///     interface depending on the signature of the method. If an invalid method signature is used, the generator will report an error.
///     <para>
///         In order to use this attribute, the containing type doesn't need to implement any interfaces. The generated properties will be lazily
///         assigned but their value will never change, so there is no need to support property change notifications or other additional functionality.
///     </para>
///     <para>
///         This attribute can be used as follows:
///         <code>
///     partial class MyViewModel
///     {
///         [ExternalEvent]
///         private void ShowGreeting(UIApplication application)
///         {
///             TaskDialog.Show("Greeting", "Hello from Revit!");
///         }
///     }
///     </code>
///         And with this, code analogous to this will be generated:
///         <code>
///     partial class MyViewModel
///     {
///         public IExternalEvent ShowGreetingEvent => field ??= new ExternalEvent(ShowGreeting);
///         public IAsyncExternalEvent ShowGreetingAsyncEvent => field ??= new AsyncExternalEvent(ShowGreeting);
///     }
///     </code>
///     </para>
///     <para>
///         <b>Methods without extra parameters.</b> The following signatures generate both sync and async properties:
///         <code>
///     void Method();
///     void Method(UIApplication);
///     </code>
///         Will generate an <see cref="IExternalEvent" /> property (using an <see cref="Nice3point.Revit.Toolkit.External.ExternalEvent" /> instance)
///         and an <see cref="IAsyncExternalEvent" /> property (using an <see cref="Nice3point.Revit.Toolkit.External.AsyncExternalEvent" /> instance).
///         <code>
///     T Method();
///     T Method(UIApplication);
///     </code>
///         Will generate an <see cref="IAsyncRequestExternalEvent{TResult}" /> property (using an <see cref="Nice3point.Revit.Toolkit.External.AsyncRequestExternalEvent{TResult}" /> instance).
///     </para>
///     <para>
///         <b>Methods with one extra parameter.</b> When a method has one additional parameter beyond the optional <c>UIApplication</c>,
///         the generator creates typed event properties:
///         <code>
///     void Method(UIApplication, string);
///     </code>
///         Will generate an <see cref="IExternalEvent{T}" /> property (using an <see cref="ExternalEvent{T}" /> instance)
///         and an <see cref="IAsyncExternalEvent{T}" /> property (using an <see cref="Nice3point.Revit.Toolkit.External.AsyncExternalEvent{T}" /> instance).
///         <code>
///     TResult Method(UIApplication, string);
///     </code>
///         Will generate an <see cref="IAsyncRequestExternalEvent{T, TResult}" /> property (using an <see cref="AsyncRequestExternalEvent{T, TResult}" /> instance).
///     </para>
///     <para>
///         <b>Methods with multiple extra parameters.</b> When a method has two or more extra parameters, the generator
///         creates a <c>sealed record</c> to bundle them into a single argument type, along with convenience extension methods:
///         <code>
///     [ExternalEvent]
///     private void DeleteInstances(UIApplication application, BuiltInCategory category, int count) { }
///     </code>
///         Will generate:
///         <code>
///     public IExternalEvent&lt;DeleteInstancesArgs&gt; DeleteInstancesEvent => field ??= new ExternalEvent&lt;DeleteInstancesArgs&gt;(...);
///     public IAsyncExternalEvent&lt;DeleteInstancesArgs&gt; DeleteInstancesAsyncEvent => field ??= new AsyncExternalEvent&lt;DeleteInstancesArgs&gt;(...);
///
///     public sealed record DeleteInstancesArgs(BuiltInCategory Category, int Count);
///
///     // Extension methods:
///     public static ExternalEventRequest Raise(this IExternalEvent&lt;DeleteInstancesArgs&gt; externalEvent, BuiltInCategory category, int count);
///     public static Task RaiseAsync(this IAsyncExternalEvent&lt;DeleteInstancesArgs&gt; externalEvent, BuiltInCategory category, int count);
///     </code>
///     </para>
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ExternalEventAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets a value indicating whether the handler should be invoked directly on the calling thread
    ///     when Revit is in API mode, instead of being queued via <see cref="ExternalEventHandler.Raise" />.
    ///     <para>
    ///         When set for an attribute used on a method that would result in an <see cref="Nice3point.Revit.Toolkit.External.ExternalEvent" />,
    ///         <see cref="Nice3point.Revit.Toolkit.External.AsyncExternalEvent" />, or <see cref="Nice3point.Revit.Toolkit.External.AsyncRequestExternalEvent{TResult}" /> property to be generated,
    ///         this will modify the behavior of these events when the handler is raised from within the Revit API context.
    ///         It is the same as creating an instance of these event types with a constructor such as
    ///         <see cref="Nice3point.Revit.Toolkit.External.ExternalEvent(System.Action, ExternalEventOptions)" /> and using the
    ///         <see cref="ExternalEventOptions.AllowDirectInvocation" /> value.
    ///     </para>
    /// </summary>
    public bool AllowDirectInvocation { get; init; }
}
