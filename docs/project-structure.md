# Project Structure

Nice3point.Revit.Toolkit is a toolkit library that simplifies Revit add-in development, plus the Roslyn tooling that ships with it. The solution separates the shipped runtime library, the analyzer/generator projects, their Roslyn-versioned twins, and the test projects. Keep code in the project that owns the runtime responsibility.

## Solution Groups

* **`/source`**: the shipped NuGet package and its bundled tooling.
    * `source/Nice3point.Revit.Toolkit`: the runtime library — the only `Dependency`-type packable project. Everything a consumer references at runtime lives here.
    * `source/Nice3point.Revit.Toolkit.Analyzers`: Roslyn diagnostic analyzers (e.g. async-void and non-partial-type checks). Targets `netstandard2.0`.
    * `source/Nice3point.Revit.Toolkit.Analyzers.CodeFixers`: code fixers that resolve the analyzer diagnostics.
    * `source/Nice3point.Revit.Toolkit.SourceGenerators`: the `[ExternalEvent]` incremental source generator.
    * `source/*.Roslyn###`: Roslyn-version twins of the three projects above. They build the **same linked source** against an older Roslyn so the package works on older IDEs/SDKs. See [Analyzers & Source Generators](./analyzers-and-generators.md).
* **`/tests`**: verification projects.
    * `tests/Nice3point.Revit.Toolkit.Tests`: TUnit + Nice3point.TUnit.Revit tests that execute inside the Revit process.
    * `tests/Nice3point.Revit.Toolkit.SourceGenerators.Tests`: generator snapshot/behavior tests using the Roslyn testing harness.
    * `tests/Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests`: analyzer + code-fixer tests with `CSharpCodeFixVerifier`.
* **`/build`**: ModularPipelines build system (`Build.csproj`) — packaging, changelog, and NuGet publishing.
* **Root level**:
    * Configuration: `Directory.Build.props`, `Directory.Packages.props`, `Directory.Roslyn.props`, `global.json`, `renovate.json`.
    * Documentation: `README.md`, `CHANGELOG.md`, `CONTRIBUTING.md`.
    * Agent guidelines: `CLAUDE.md`, `AGENTS.md`, `.junie/AGENTS.md`, `docs/`.
    * CI/CD: `.github/workflows`.

## Source Layout

The runtime library groups types by responsibility:

* `source/Nice3point.Revit.Toolkit/Context.cs`, `RevitApiContext.cs`, `RevitContext.cs`: static global access to the Revit Application (DB level) and UI Application, plus API-mode detection and dialog/failure suppression scopes.
* `source/Nice3point.Revit.Toolkit/External/Commands`: `ExternalCommand` / `AsyncExternalCommand` base classes for `IExternalCommand`.
* `source/Nice3point.Revit.Toolkit/External/Applications`: `ExternalApplication`, `AsyncExternalApplication`, `ExternalDBApplication` base classes.
* `source/Nice3point.Revit.Toolkit/External/Events`: the `ExternalEvent` family (sync, async, request/response, generic), their interfaces under `Interfaces`, and the `[ExternalEvent]` attribute under `Attributes`.
* `source/Nice3point.Revit.Toolkit/External/Handlers`: legacy `IExternalEventHandler`-based handlers (e.g. `ActionEventHandler`) — mostly `[Obsolete]` in favor of `ExternalEvent`.
* `source/Nice3point.Revit.Toolkit/Options`: Revit API callback/option implementations (e.g. `FamilyLoadOptions`, `DuplicateTypeNamesHandler`).
* `source/Nice3point.Revit.Toolkit/Decorators`: fluent wrappers such as `DockablePaneProvider`.
* `source/Nice3point.Revit.Toolkit/Helpers`: cross-cutting helpers (e.g. `ResolveHelper` assembly resolution).
* `source/Nice3point.Revit.Toolkit/Utils`: utility classes.
* `source/Nice3point.Revit.Toolkit/Internal`: non-public reflection accessors. Never part of the public API.

## Change Placement

* Put a new base class for a Revit add-in entry point in `External/Commands` or `External/Applications`.
* Put external-event types in `External/Events`; put the matching interface in `External/Events/Interfaces`.
* Put Revit API option/callback implementations in `Options`.
* Put reflection-based or otherwise unsafe internals in `Internal`; never expose them publicly.
* Put a new analyzer in `Analyzers`, its fixer in `Analyzers.CodeFixers`, and generator logic in `SourceGenerators` — the `.Roslyn###` twins pick the files up automatically via linked compilation.
* Put Revit-thread coverage in `tests/Nice3point.Revit.Toolkit.Tests`, and Roslyn tooling coverage in the matching `*.Tests` project.
