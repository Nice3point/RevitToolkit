# Nice3point.Revit.Toolkit Agent Instructions

Nice3point.Revit.Toolkit is a public NuGet library that removes the boilerplate of building Revit add-ins. `source/Nice3point.Revit.Toolkit` is the shipped runtime library: base classes for external commands and applications, static Application and UI contexts, async support, external event handlers, options, decorators, and helpers. The package also ships bundled Roslyn analyzers, code fixers, and a source generator under `source/` that automate the external-event boilerplate and guard against misuse.

## Non-Negotiables

* **Never break an existing public API.** Deprecate with `[Obsolete]` plus `CodeTemplate`. The obsolete member keeps calling the original implementation and stays functional. See [Backward Compatibility](./docs/backward-compatibility.md).
* **Instance classes with inheritance for entry points.** External commands and applications override a simplified `Execute()`. Global contexts are static classes. Boilerplate lives in the base class, never in the consumer's override.
* **Scoped operations return `IDisposable`** through the `Begin...Scope()` pattern. Restore or release on `Dispose`.
* **Attributes carry the contract.** Mark public classes `[PublicAPI]`, Revit-invoked callbacks `[EditorBrowsable(EditorBrowsableState.Never)]`, and read-only methods `[Pure]`.
* **Thread safety is mandatory for shared state.** Synchronize with `Lock`, guard dispose with `Interlocked.Exchange`, count nested scopes under a lock, and queue cross-thread work through `ExternalEvent` or `ConcurrentQueue`.
* **Every type compiles under every supported configuration.** Gate version-specific Revit APIs with `#if REVIT2024_OR_GREATER`-style directives and runtime features with `#if NET` or `#if NET8_0_OR_GREATER`.
* **Roslyn tooling is public surface.** Analyzers, fixers, and the generator target `netstandard2.0`, dual-target Roslyn (5.0 default plus `*.Roslyn414`), use `RVTTK####` diagnostic ids, and need an `AnalyzerReleases.Unshipped.md` entry for every new or changed rule.
* **Tests ship with every change.** Test custom logic only: Revit behavior on the Revit thread, analyzers and generators through the Roslyn testing harness. See [Testing](./docs/testing.md).
* **Verify unfamiliar APIs.** When unsure of a Revit or .NET API's behavior or signature, confirm it before use. Search the web for the official docs. To read a referenced library's source, query GitHub with `gh` (`gh api`, `gh search code`). If `gh` is unavailable, search the web or ask. Never inspect compiled DLLs or XML extracted from NuGet packages.
* **Keep docs in sync.** A public-surface change updates `README.md`, `CHANGELOG.md`, and the XML docs in the same commit. See [Documentation](./docs/documentation.md).

## Build

The build is a ModularPipelines project. Run `dotnet run -c Release` from the `build` directory to compile.

## Specialized Docs

Read the matching file before related work.

* [Project Structure](./docs/project-structure.md). Solution layout, project grouping, and change placement.
* [Architecture](./docs/architecture.md). Design goals, contexts, the base-class model, async, and disposable scopes.
* [Code Style](./docs/code-style.md). Naming, attributes, language features, the scope and event-handler patterns, and error handling.
* [Backward Compatibility](./docs/backward-compatibility.md). The Obsolete plus CodeTemplate pattern and breaking-change rules.
* [Testing](./docs/testing.md). Revit-thread tests and Roslyn analyzer, fixer, and generator tests.
* [Revit Best Practices](./docs/revit-best-practices.md). Revit API usage, the version matrix, threading, and performance.
* [Analyzers & Source Generators](./docs/analyzers-and-generators.md). The Roslyn projects, diagnostics, Roslyn version targeting, and packaging.
* [Documentation](./docs/documentation.md). README, CHANGELOG, and XML documentation rules.
* [Package Management](./docs/package-management.md). Centralized NuGet and Revit-version package rules.
