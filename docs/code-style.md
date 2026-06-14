# Strict C# Production Style

All code must meet production-quality standards. "It works" is not enough; code must be clean, readable, and self-explanatory. This is a public library — its style is part of its API.

## General Principles

* **Modern C#:** target the latest language version (`LangVersion=latest`). Nullable reference types and implicit usings are enabled solution-wide.
* **Instance vs. static:** instance classes with inheritance for external commands/applications; static classes for global contexts.
* **Disposable scopes:** model temporary state changes as `IDisposable` returned from a `Begin...Scope()` factory.
* **Pure functions:** mark read-only operations with `[Pure]`.
* **Explicit over implicit:** code should be self-explanatory; avoid hidden behavior and unclear defaults.
* **JetBrains Annotations:** use `[PublicAPI]`, `[Pure]`, and `CodeTemplate` where they improve analysis and intent.

## Naming

* **Clarity is king.** Names must be descriptive and never abbreviated.
    * Bad: `elem`, `doc`, `param`, `app`, `ctx`.
    * Good: `element`, `document`, `parameter`, `application`, `context`.
* **Follow Revit API naming conventions.**
* **Scope factories use the `Begin...Scope` pattern:**
    * Good: `BeginFailureSuppressionScope()`, `BeginDialogSuppressionScope()`, `BeginAssemblyResolveScope()`.
    * Bad: `SuppressFailures()`, `SuppressDialogs()`.
* Avoid single-letter variables except in very short loops or lambdas.

## File & Class Structure

* **File-scoped namespaces.** Use `namespace Nice3point.Revit.Toolkit;` or a sub-namespace. When a file lives in a subfolder but should keep a flatter namespace (e.g. `Nice3point.Revit.Toolkit.External`), put `// ReSharper disable once CheckNamespace` above the declaration.
* **`[PublicAPI]`** on every public class.
* **`[EditorBrowsable(EditorBrowsableState.Never)]`** on members Revit invokes but consumers should not call (the interface `Execute`, handler callbacks).
* **`[Pure]`** on read-only methods.

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

## XML Documentation

* Document every public member with `<summary>`, parameters with `<param>`, and return values with `<returns>`.
* Use `<remarks>` for implementation details, constraints, and thread-safety notes; `<example>` for non-trivial APIs.
* For wrappers over the Revit API, mirror the corresponding Revit API documentation and document the Revit `<exception>`s the member can throw.
* Keep comments concise and update them in the same change as the behavior.

## Error Handling

* **Let Revit exceptions propagate** by default; never swallow them. Document them with `<exception>`.
* **Optional exception handler:** for handlers that process multiple queued actions, provide a `SetExceptionHandler()` hook rather than swallowing silently.
* **Validate custom logic** with internal guards (the `ThrowWhen()` pattern), not thin wrappers where Revit already validates.

## Compilation Directives

* `#if REVIT2024_OR_GREATER` (and similar) for version-specific Revit APIs.
* `#if NET` / `#if NET8_0_OR_GREATER` for runtime-specific features (e.g. `AssemblyLoadContext`, `UnsafeAccessor`).
* Apply directives consistently across related members so a type's surface stays coherent per version. See [Revit Best Practices](./revit-best-practices.md).
