# Analyzers & Source Generators

The package bundles Roslyn tooling that ships alongside the runtime library: a source generator that emits external-event boilerplate, analyzers that enforce its prerequisites, and code fixers that resolve the diagnostics. This tooling is part of the public contract — its generated API shape and diagnostic ids are stable surface.

## Projects

* `source/Nice3point.Revit.Toolkit.SourceGenerators`: the `[ExternalEvent]` incremental generator.
* `source/Nice3point.Revit.Toolkit.Analyzers`: diagnostic analyzers and the `DiagnosticDescriptors` catalog.
* `source/Nice3point.Revit.Toolkit.Analyzers.CodeFixers`: code fixers that resolve those diagnostics.
* `source/*.Roslyn###`: Roslyn-version twins (see below).

All target `netstandard2.0` with `IsRoslynComponent=true` and `EnforceExtendedAnalyzerRules=true` (configured in `Directory.Roslyn.props`).

## The `[ExternalEvent]` Generator

Decorate a method on a `partial` type with `[ExternalEvent]`; the generator emits the matching external-event property, picking the shape (sync vs. request/response, parameterless vs. parameterized, single argument vs. a generated argument record) from the method signature.

```csharp
partial class MyViewModel
{
    [ExternalEvent]
    private void ShowGreeting(UIApplication application) { /* ... */ }
}
// generates ShowGreetingEvent / ShowGreetingAsyncEvent properties
```

The `ExternalEventAttribute` XML docs are the source of truth for the full signature-to-output matrix and options such as `AllowDirectInvocation` — keep them current there, not duplicated here.

## Diagnostics

Diagnostics use the `RVTTK####` id prefix and are defined in `Analyzers/Diagnostics/DiagnosticDescriptors.cs`, which is the source of truth for the current set. They enforce the generator's prerequisites — for example, constraints on the signature and declaration of a method marked `[ExternalEvent]`.

When adding or changing a rule:

* Assign the next unused `RVTTK####` id — **never** reuse a retired id.
* Add a `DiagnosticDescriptor` with a clear `title`, a parameterized `messageFormat`, and an appropriate `category`/severity.
* Record it in `source/Nice3point.Revit.Toolkit.Analyzers/AnalyzerReleases.Unshipped.md`.
* Add a code fixer when the violation is mechanically resolvable, and cover both in the test projects. See [Testing Strategy](./testing-strategy.md).

## Roslyn Version Targeting

Each tooling project has a `.Roslyn###` twin. `Directory.Roslyn.props` parses the version suffix, **links the base project's `.cs` files** into the twin, and sets `ROSLYN*_OR_GREATER` constants. This means:

* Author code once in the base project; the twin compiles the same source against the older Roslyn.
* Guard any API that differs across Roslyn versions with the `ROSLYN*_OR_GREATER` constants.
* Do not duplicate source into a twin — it is intentionally empty except for its project file.

## Packaging

The runtime `.csproj` packs each tooling assembly into the version-specific Roslyn analyzer paths so the matching build loads per IDE/SDK. It also references the tooling projects with `ReferenceOutputAssembly="false"` purely to enforce build order. See [Package Management](./package-management.md).
