# Revit Best Practices

The toolkit runs inside the Revit process and manages Revit's threading and lifecycle constraints on the consumer's behalf. Respect the API's rules and stay allocation-conscious on hot paths.

## Wrap the Revit API

* The toolkit wraps the Revit API and manages lifecycle. It does not reimplement Revit behavior.
* Read-only operations are `[Pure]`. Let Revit exceptions propagate and document them. See [Code Style](./code-style.md).
* Reflection into Revit internals lives in `Internal/Unsafe*Accessors` and stays non-public. Do not surface it.

## Threading & the API Context

* The Revit API may only be modified from the Revit API context. Use `RevitContext.IsRevitInApiMode` to decide between a direct call and queuing work.
* Marshal cross-thread Revit work through the `ExternalEvent` family (or the `[ExternalEvent]` source generator), with the fast path that invokes directly when already in API mode.
* Suppress UI during programmatic operations with `RevitContext.BeginDialogSuppressionScope()` or `RevitApiContext.BeginFailureSuppressionScope()`, and always restore through the disposable scope.

## Revit Versions

The active version comes from the `$(RevitVersion)` build property. The project (SDK `Nice3point.Revit.Sdk`) declares the full `Debug.RNN`/`Release.RNN` configuration list.

* Use conditional compilation (`#if REVIT2024_OR_GREATER`, and similar) only where the Revit API genuinely differs between versions.
* Use `#if NET` or `#if NET8_0_OR_GREATER` for runtime differences, such as `AssemblyLoadContext`-aware assembly resolution in `ExternalCommand`.
* Apply directives consistently across related members so a type's surface stays coherent per version.
* Every type must compile under every declared `Debug.RNN`/`Release.RNN` configuration.
* Version-specific package versions belong in `Directory.Packages.props`. See [Package Management](./package-management.md).

## Thread Safety

* **Lock objects:** synchronize shared state with the `Lock` type. Keep lock sections minimal.
* **Interlocked:** guard dispose flags with `Interlocked.Exchange`.
* **Scope counting:** track nested scopes with a counter mutated under the lock.
* **Continuations:** use `TaskScheduler.Default` for continuations to avoid deadlocks on the Revit thread.
* **Queuing:** use `ConcurrentQueue` for thread-safe action queuing in handlers.

## Performance

* **Avoid LINQ on hot paths.** Use traditional loops where allocations or iterator overhead matter.
* **Pre-size collections** when the count is known.
* **Prefer batch Revit APIs** over per-element calls, and minimize transaction scope.
* **Unsubscribe** from Revit events when a scope ends.

## Internal Helpers

* Reflection-based accessors live in `source/Nice3point.Revit.Toolkit/Internal` and stay non-public.
* Native interop (function-pointer marshaling for the API-context scope) is encapsulated in private nested scope types. Do not expose the pointers or delegates.
