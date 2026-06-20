# Architecture & Design Principles

Nice3point.Revit.Toolkit exists to remove the boilerplate of building Revit add-ins. It wraps the awkward parts of the Revit API (the `IExternalCommand`/`IExternalApplication` plumbing, external events, the threading model, assembly resolution, and option callbacks) behind clean, discoverable, production-grade types.

## Core Design Goals

* **Static context access:** expose the Revit Application (DB level) and UI Application globally through static `Context`/`RevitApiContext`/`RevitContext` classes, so any code can reach the active document, view, or UI application without threading it through every call.
* **Simplification:** absorb the Revit interface boilerplate (`IExternalCommand`, `IExternalApplication`, `IExternalEventHandler`) into base classes that expose a single override.
* **Async support:** enable `async`/`await` inside external commands and event handlers via `DispatcherFrame` message pumping and the `ExternalEvent` family.
* **Thread safety:** all shared state is safe to touch from any thread. Cross-thread Revit work is marshaled to the Revit API context.
* **Disposable scopes:** model temporary state changes (dialog suppression, failure suppression, assembly resolution) as `IDisposable` scopes.
* **Backward compatibility:** never break an existing public API. See [Backward Compatibility](./backward-compatibility.md).

## Contexts

`RevitApiContext` exposes the DB-level `Application`. `RevitContext` (its subclass) adds the `UIApplication`, active document/view accessors, `IsRevitInApiMode`, and the dialog-suppression scopes. They are static and initialized via reflection against Revit's internals (`Internal/Unsafe*Accessors`), so consumers get global access without an `ExternalCommandData` in hand.

* `RevitContext.IsRevitInApiMode` decides whether a Revit call can run directly or must be queued through an external event.
* Use `RevitApiContext.BeginFailureSuppressionScope()` / `RevitContext.BeginDialogSuppressionScope()` to suppress UI during programmatic operations.

## Base-Class Model

External commands/applications are **instance classes** consumers inherit. The base class implements the Revit interface, performs setup (assembly-resolution scope, populating `Application`/`View`/`JournalData`), then calls the consumer's `abstract void Execute()`. The Revit-facing `Execute(ExternalCommandData, ref string, ElementSet)` is marked `[EditorBrowsable(Never)]` so it never shows up in the consumer's IntelliSense.

```csharp
public abstract class ExternalCommand : IExternalCommand
{
    public Result Result { get; set; } = Result.Succeeded;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // populate Application/View/JournalData, then:
        using (ResolveHelper.BeginAssemblyResolveScope(GetType()))
        {
            Execute();
        }
        return Result;
    }

    public abstract void Execute();
}
```

## External Events & the Source Generator

The Revit API can only be modified from the API context. `ExternalEvent` (and its async/request/generic variants) wraps `Autodesk.Revit.UI.ExternalEvent` so add-ins can request document changes from any thread, with a fast path that invokes the handler directly when `IsRevitInApiMode` is true.

The `[ExternalEvent]` source generator removes the per-handler boilerplate by emitting the matching `IExternalEvent`/`IAsyncExternalEvent` property for an annotated `partial`-type method, choosing the form from the method signature. The analyzers enforce its prerequisites. See [Analyzers & Source Generators](./analyzers-and-generators.md).

## Disposable Scopes

Temporary state is modeled as a scope: a `Begin...Scope()` factory acquires the resource (subscribes to an event, increments a counter, allocates native memory) and returns an `IDisposable` that reverses it. Scopes are reference-counted under a lock so they nest safely. See the canonical implementation in [Code Style](./code-style.md).

## Design Rules

* Wrap the Revit API rather than reimplement it. Toolkit types add ergonomics and lifecycle management, not new behavior.
* Keep the public surface in `[PublicAPI]`-marked classes. Keep reflection internals in `Internal` and never leak them.
* Mark read-only operations `[Pure]` and Revit-invoked callbacks `[EditorBrowsable(Never)]`.
* Isolate version-specific Revit API differences behind compilation directives, not duplicated types. See [Revit Best Practices](./revit-best-practices.md).
