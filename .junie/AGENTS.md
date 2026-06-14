# Nice3point.Revit.Toolkit Agent Instructions

Nice3point.Revit.Toolkit is a public NuGet library that simplifies building Revit add-ins. `source/Nice3point.Revit.Toolkit` is the shipped runtime library — base classes for external commands and applications, static Application/UI contexts, async/await support, external event handlers, options, decorators, and helpers. The package also ships bundled Roslyn analyzers, code fixers, and a source generator (separate projects under `source/`) that automate the external-event boilerplate and guard against misuse.

## Non-Negotiables

* Never break an existing public API. Deprecate with `[Obsolete]` + `CodeTemplate`; the obsolete member must keep calling the original implementation and stay functional.
* Use instance classes with inheritance for external commands/applications (override a simplified `Execute()`); use static classes for global contexts. Boilerplate lives in the base class, never in the consumer's override.
* Scoped operations return `IDisposable` via the `Begin...Scope()` pattern (`BeginDialogSuppressionScope`, `BeginFailureSuppressionScope`, `BeginAssemblyResolveScope`). Restore/release on `Dispose`.
* Mark public classes `[PublicAPI]`, Revit-invoked callbacks `[EditorBrowsable(EditorBrowsableState.Never)]`, and read-only methods `[Pure]`.
* Thread safety is mandatory for shared state: synchronize with `Lock`, guard dispose with `Interlocked.Exchange`, count nested scopes under a lock, and queue cross-thread work via `ExternalEvent` / `ConcurrentQueue`.
* Every type must compile under every supported `Debug.RNN`/`Release.RNN` configuration the project declares. Gate version-specific Revit APIs with `#if REVIT2024_OR_GREATER`-style directives and runtime features with `#if NET` / `#if NET8_0_OR_GREATER`.
* Analyzers, code fixers, and the source generator target `netstandard2.0`, dual-target Roslyn (5.0 default + `*.Roslyn414`), use `RVTTK####` diagnostic ids, and require an `AnalyzerReleases.Unshipped.md` entry for every new/changed rule.
* Test only custom logic: Revit behavior in `tests/Nice3point.Revit.Toolkit.Tests` (TUnit + Nice3point.TUnit.Revit on the Revit thread); analyzers/fixers/generators via the Roslyn testing harness in their `*.Tests` projects.
* Update `README.md`, `CHANGELOG.md`, and XML docs in the same change as any public-surface change.

## Specialized Docs

Before making related changes, read the matching file:

* [Project Structure](../docs/project-structure.md) - solution layout, project grouping, and change placement.
* [Architecture](../docs/architecture.md) - design goals, contexts, base-class model, async, and disposable scopes.
* [Code Style](../docs/code-style.md) - class/naming conventions, scope and event-handler patterns, file structure, XML docs, and error handling.
* [Backward Compatibility](../docs/backward-compatibility.md) - the Obsolete + CodeTemplate pattern and breaking-change rules.
* [Documentation](../docs/documentation.md) - README, Changelog, and XML documentation requirements.
* [Testing Strategy](../docs/testing-strategy.md) - Revit-thread tests and Roslyn analyzer/fixer/generator tests.
* [Revit Best Practices](../docs/revit-best-practices.md) - Revit API usage, version matrix, threading, performance, and internal helpers.
* [Analyzers & Source Generators](../docs/analyzers-and-generators.md) - the Roslyn projects, diagnostics, Roslyn version targeting, and packaging.
* [Package Management](../docs/package-management.md) - centralized NuGet and Revit-version package rules.
