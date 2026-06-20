# Code Style

Production C# only. This is a public library, so its style is part of its contract.

## General Principles

* **SOLID and DRY.** One responsibility per type. Extract shared logic rather than duplicate it.
* **Explicit over implicit.** Code is self-explanatory. Avoid hidden behavior and unclear defaults.
* **Instance vs. static.** Instance classes with inheritance for external commands and applications. Static classes for global contexts.
* **Disposable scopes.** Model a temporary state change as an `IDisposable` returned from a `Begin...Scope()` factory.
* **Nullable safety.** Nullable reference types are enabled solution-wide. Treat every nullability warning as a defect.
* **StyleCop style.** Follow StyleCop conventions for layout, member ordering, and spacing.

## Modern C#

`LangVersion` is `latest`. Reach for the newest feature that expresses the intent directly, and do not hand-roll what the language already provides.

* Primary constructors when a type captures state.
* Collection expressions for literals and spans.
* Pattern matching and switch expressions over branching chains.
* Range and index operators for slicing.
* Null-coalescing assignment (`??=`) for lazy initialization.
* Expression-bodied members for simple accessors.
* File-scoped namespaces.

## Comments

Public types and members carry XML doc comments, see [Documentation](./documentation.md). Inside the code, comments are the exception.

* Names and structure carry the meaning. Default to no comment.
* Add one only when the reason cannot be read from the code and a reader could break the code without it, such as a non-obvious invariant or a threading constraint.
* A comment explains why, never what. Do not restate the code.

## Attributes

Decorate members with every JetBrains and .NET attribute that carries meaning, so analyzers, the debugger, and callers read the full contract.

* `[PublicAPI]` on every public class.
* `[Pure]` on a read-only method.
* `[EditorBrowsable(EditorBrowsableState.Never)]` on a member Revit invokes but consumers must not call, such as the interface `Execute` and handler callbacks.
* `[CodeTemplate]` on a deprecated member so Rider can auto-convert call sites. See [Backward Compatibility](./backward-compatibility.md).

## Naming

* **Clarity first.** Names are descriptive and never abbreviated: `element` not `elem`, `document` not `doc`, `parameter` not `param`, `application` not `app`, `context` not `ctx`.
* Follow the Revit API naming conventions.
* Scope factories use the `Begin...Scope` pattern: `BeginFailureSuppressionScope()`, not `SuppressFailures()`.
* No single-letter variables except in a short loop or lambda.

## File and Class Structure

* **File-scoped namespaces.** Use `namespace Nice3point.Revit.Toolkit;` or a sub-namespace. When a file lives in a subfolder but keeps a flatter namespace, add `// ReSharper disable once CheckNamespace` above the declaration.
* **Member order:** private fields, constructors, public properties, public methods, private methods.

## Disposable Scope Pattern

A scope factory acquires a resource and returns an `IDisposable` that reverses it. Reference-count nested scopes under a lock and guard `Dispose` with `Interlocked.Exchange`:

```csharp
public static IDisposable BeginSomeScope()
{
    lock (SomeLock)
    {
        if (_scopeCount++ == 0)
        {
            // Subscribe to events or acquire resources
        }
    }

    return new SomeScope();
}

private sealed class SomeScope : IDisposable
{
    private int _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        lock (SomeLock)
        {
            if (--_scopeCount == 0)
            {
                // Unsubscribe from events or release resources
            }
        }
    }
}
```

## External Command/Application Pattern

The base class handles all Revit plumbing and exposes one override. Wrap user code in an assembly-resolution scope so dependencies load from the add-in's folder:

```csharp
public abstract class SomeExternalCommand : IExternalCommand
{
    public Result Result { get; set; } = Result.Succeeded;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        using (ResolveHelper.BeginAssemblyResolveScope(GetType()))
        {
            Execute();
        }
        return Result;
    }

    public abstract void Execute();
}
```

## Async Command Pattern

Pump messages with `DispatcherFrame` so `await` works inside a synchronous Revit command:

```csharp
public sealed override void Execute()
{
    var task = ExecuteAsync();
    if (task.IsCompleted)
    {
        task.GetAwaiter().GetResult();
        return;
    }

    var frame = new DispatcherFrame();
    task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
    Dispatcher.PushFrame(frame);
    task.GetAwaiter().GetResult();
}
```

## Event Handler Pattern

Queue cross-thread work, but take the fast path when already inside the Revit API context:

```csharp
public void Raise(Action<UIApplication> action)
{
    if (RevitContext.IsRevitInApiMode)
    {
        action(RevitContext.UiApplication);
        return;
    }

    _queue.Enqueue(action); // ConcurrentQueue for thread safety
    Raise();
}
```

## Error Handling

* **Let Revit exceptions propagate** by default. Never swallow them. Document them with `<exception>`.
* **Optional exception handler.** For a handler that processes multiple queued actions, provide a `SetExceptionHandler()` hook rather than swallow silently.
* **Validate custom logic** with internal guards (the `ThrowWhen()` pattern), not thin wrappers where Revit already validates.

## Compilation Directives

* `#if REVIT2024_OR_GREATER` and similar for version-specific Revit APIs.
* `#if NET` or `#if NET8_0_OR_GREATER` for runtime-specific features such as `AssemblyLoadContext` and `UnsafeAccessor`.
* Apply directives consistently across related members so a type's surface stays coherent per version. See [Revit Best Practices](./revit-best-practices.md).
