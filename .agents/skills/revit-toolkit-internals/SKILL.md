---
name: revit-toolkit-internals
description: >
  Uphold the design contract of the Nice3point.Revit.Toolkit runtime library, that wraps the raw Revit add-in API.
  USE FOR: adding or changing a runtime type in the External, External/Events, Options, Helpers, or Internal folders, or the static RevitApiContext/RevitContext; deciding where boilerplate lives and how a scope, handler, or context is shaped.
  DO NOT USE FOR: authoring the bundled Roslyn analyzers, code fixers, or the source generator, which are a separate compiler tooling contract.
license: MIT
---

# Revit Toolkit Internals

Nice3point.Revit.Toolkit removes add-in boilerplate by wrapping the awkward parts of the Revit API behind clean instance and static types.
The library adds ergonomics and lifecycle management, never new Revit behavior.
This skill is the design contract a maintainer upholds when adding or changing a runtime type.

## When to use

- Adding or changing a base class under `External/Commands` or `External/Applications`, an event type under `External/Events`, an option handler under `Options`, a helper under `Helpers`, or an accessor under `Internal`.
- Changing the static `RevitApiContext` or `RevitContext`, a `Begin...Scope()` operation, or a cross-thread handler.

## When not to use

- Authoring the bundled Roslyn analyzers, code fixers, or the `[ExternalEvent]` source generator. That is the compiler tooling contract with its own `RVTTK####` ids and dual-Roslyn packaging.

## Workflow

### Step 1: Choose instance base class or static context

Model an add-in entry point as an **instance base class** the consumer inherits; model a global service as a **static class**.
A base class implements the Revit interface, marks the Revit-facing method `[EditorBrowsable(EditorBrowsableState.Never)]`, does the setup, and exposes a single `abstract` override; the consumer writes no plumbing.

```csharp
[PublicAPI]
public abstract class ExternalCommand : IExternalCommand
{
    public UIApplication Application { get; private set; } = null!;
    public Result Result { get; set; } = Result.Succeeded;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Application = commandData.Application;
        using (ResolveHelper.BeginAssemblyResolveScope(GetType()))
        {
            Execute();
        }
        return Result;
    }

    public abstract void Execute();
}
```

`RevitApiContext` (DB-level `Application`) and its subclass `RevitContext` (UI-level) are static, and their static constructors reflect against Revit internals.
Keep every consumer-visible entry point's boilerplate in the base class, never in the override.

### Step 2: Model temporary state as a disposable Begin...Scope()

A temporary state change (dialog suppression, failure suppression, assembly resolution, the API context) is a factory named `Begin...Scope()` that acquires the resource and returns an `IDisposable` reversing it.
Reference-count nested scopes under a `Lock`, and guard `Dispose` with `Interlocked.Exchange`; a double dispose is a no-op.

```csharp
private static readonly Lock FailureLock = new();
private static int _failureScopeCount;

public static IDisposable BeginFailureSuppressionScope(bool resolveErrors = true)
{
    lock (FailureLock)
    {
        _suppressFailureErrors = resolveErrors;
        if (_failureScopeCount++ == 0)
        {
            Application.FailuresProcessing += ResolveFailures;
        }
    }

    return new FailureSuppressionScope();
}

private sealed class FailureSuppressionScope : IDisposable
{
    private int _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        lock (FailureLock)
        {
            if (--_failureScopeCount == 0)
            {
                Application.FailuresProcessing -= ResolveFailures;
            }
        }
    }
}
```

Name the factory for the action it begins (`BeginFailureSuppressionScope`, not `SuppressFailures`), and always unsubscribe from the Revit event when the last scope closes.

### Step 3: Keep shared state thread-safe

Every static context is reachable from any thread; all shared state is synchronized.
Use the `Lock` type for shared fields, keep lock sections minimal, track nested scopes with a counter mutated under the lock, and guard dispose flags with `Interlocked.Exchange`.
Read a shared value under the lock into a local before acting on it, as the failure and dialog resolvers do; do not touch the field outside the lock.

### Step 4: Marshal cross-thread Revit work through an external event

The Revit API may only be modified from the API context.
Wrap `Autodesk.Revit.UI.ExternalEvent`; a handler raised from any thread runs in the API context. Take a fast path that invokes the handler directly when already in API mode.

```csharp
public override ExternalEventRequest Raise()
{
    if ((_options & ExternalEventOptions.AllowDirectInvocation) != 0 && RevitContext.IsRevitInApiMode)
    {
        Execute(RevitContext.UiApplication);
        return ExternalEventRequest.Accepted;
    }

    return base.Raise();
}
```

Await completion with a `TaskCompletionSource` (see `AsyncExternalEvent.RaiseAsync`), and queue cross-thread work with a thread-safe structure such as `ConcurrentQueue` when a handler batches multiple actions.
The base `ExternalEventHandler` constructor creates the underlying Revit event inside `RevitContext.BeginApiContextScope()`; construction is valid off the API context.

### Step 5: Pump messages with DispatcherFrame for async

`async`/`await` inside a synchronous Revit command works by pumping a `DispatcherFrame`.
Complete the frame from a continuation scheduled on `TaskScheduler.Default`, not the UI synchronization context, or the continuation deadlocks against `PushFrame`.

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

### Step 6: Confine native and reflection interop to Internal/

Reflection into Revit internals and native function-pointer marshaling stay non-public and never leak.
On modern runtimes the accessors use `[UnsafeAccessor]` (gated `#if NET8_0_OR_GREATER`, with a reflection fallback for the older target); function-pointer interop lives inside a private nested scope type.

```csharp
internal static class UnsafeAccessors
{
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    internal static extern Application CreateApplication(object proxy);
}
```

Never surface a pointer, delegate, or reflected member from `Internal/`; the API-context scope allocates and frees its native memory inside its own `Dispose`.

### Step 7: Attribute the surface, gate the versions, then verify

Mark every public class `[PublicAPI]`, every member Revit invokes but consumers must not call `[EditorBrowsable(EditorBrowsableState.Never)]`, and every read-only method `[Pure]`.
Wrap an option callback interface with ergonomic constructors, like `FamilyLoadOptions : IFamilyLoadOptions` and `DuplicateTypeNamesHandler : IDuplicateTypeNamesHandler`; keep the Revit-invoked method non-browsable.
Every type must compile under every declared `Debug.R20`..`R27` / `Release.R20`..`R27`; gate version-specific Revit APIs with `#if REVIT2024_OR_GREATER`-style directives and runtime features (`AssemblyLoadContext`, `UnsafeAccessor`) with `#if NET` or `#if NET8_0_OR_GREATER`.
Ship a test for the changed custom logic in `tests/Nice3point.Revit.Toolkit.Tests`, then build.

```shell
dotnet test -c Debug.R27
```

## Validation

- [ ] Entry points are instance base classes with one `abstract` override; global contexts are static classes; boilerplate lives in the base, not the override.
- [ ] A temporary state change is a `Begin...Scope()` returning `IDisposable`, reference-counted under a `Lock`, with an `Interlocked.Exchange` dispose guard.
- [ ] All shared static state is synchronized; the Revit event is unsubscribed when the last scope closes.
- [ ] Cross-thread Revit work is marshaled through the external event with the API-mode fast path; async pumps a `DispatcherFrame` with a `TaskScheduler.Default` continuation.
- [ ] Reflection and native interop stay in `Internal/` and are never surfaced.
- [ ] Public classes carry `[PublicAPI]`, Revit callbacks `[EditorBrowsable(Never)]`, read-only methods `[Pure]`; every type compiles under every `Debug.RNN`/`Release.RNN`.
- [ ] A test covers the changed custom logic.

## Common Pitfalls

| Pitfall                                            | Correct approach                                                                   |
|----------------------------------------------------|------------------------------------------------------------------------------------|
| Putting Revit plumbing in the consumer's override  | Keep setup in the base class; expose one `abstract` method.                        |
| A scope that forgets to reverse or double-reverses | Count under a `Lock` and guard `Dispose` with `Interlocked.Exchange`.              |
| Touching a shared field outside the lock           | Read it under the lock into a local, then act.                                     |
| Calling the Revit API off the API context          | Marshal through the external event; fast-path only when `IsRevitInApiMode`.        |
| Completing the async frame on the UI context       | Continue on `TaskScheduler.Default`; the UI context deadlocks against `PushFrame`. |
| Exposing a reflected member or native pointer      | Keep it `internal` in `Internal/`; expose only the wrapped type.                   |
| A version-specific API without a compilation gate  | Gate with `#if REVIT####_OR_GREATER` / `#if NET*` so every configuration compiles. |
