# Nice3point.Revit.Toolkit

Nice3point.Revit.Toolkit is a public NuGet library that removes the boilerplate of building Revit add-ins.
The runtime library wraps the awkward parts of the Revit API behind clean instance base classes and static contexts.
The same package bundles Roslyn tooling: an incremental source generator that emits external-event boilerplate, analyzers that enforce its prerequisites, and code fixers that resolve the diagnostics.

## Non-negotiables

* Model an add-in entry point as an instance base class the consumer inherits, and a global context as a static class. All plumbing lives in the base class behind a single override, never in the consumer's code.
* Model a temporary state change as an `IDisposable` returned from a `Begin...Scope()` factory that reverses it on `Dispose`.
* Shared static state is thread-safe.
* Reflection and native interop stay non-public in `Internal/`.
* Never break the public surface. Deprecate a renamed member with `[Obsolete]` with a JetBrains `[CodeTemplate]` auto-conversion; the obsolete member forwards to the replacement and never throws or changes behavior.
* Mark a member Revit invokes but consumers must not call `[EditorBrowsable(EditorBrowsableState.Never)]`.
* The Roslyn tooling is public surface. It targets `netstandard2.0`, dual-targets Roslyn (the default with the `.Roslyn414` twins), uses stable `RVTTK####` diagnostic ids, and needs an `AnalyzerReleases.Unshipped.md` entry per new or changed rule. Never rename a generated member or renumber a shipped id.
* Every type compiles under every supported configuration.
* A change ships with a test covering the toolkit's custom logic: Revit behavior runs on the Revit thread, and the analyzers, fixers, and generator run against the Roslyn testing harness.
* Confirm an unfamiliar Revit or .NET API before use through official docs or `gh` (`gh api`, `gh search code`).
* A public-surface change updates `README.md`, `CHANGELOG.md`, and the XML docs in the same commit.

## Repository map

* `source/Nice3point.Revit.Toolkit` it packs the runtime library and the tooling into one NuGet package for users.
* `source/Nice3point.Revit.Toolkit.Analyzers`, `Nice3point.Revit.Toolkit.Analyzers.CodeFixers`, and `Nice3point.Revit.Toolkit.SourceGenerators` hold the analyzers, code fixers, and source generator, each with a `.Roslyn414` twin for the older Roslyn.
* `tests/` — `Nice3point.Revit.Toolkit.Tests` runs inside a Revit process; the analyzer, fixer, and generator test projects run against the compiler.
* `build/` — the ModularPipelines build.
* Root — `Directory.Build.props`, `Directory.Packages.props`, `Directory.Roslyn.props`, `global.json`, the `AnalyzerReleases.*.md` release tracking, `README.md`, `CHANGELOG.md`.

## Build and verify

* Build: `dotnet build -c Release.R##`, where the `R##` suffix is the Revit year (`R27` targets Revit 2027).
* Test: `dotnet test -c Release.R##`; requires a matching licensed Revit installation.
