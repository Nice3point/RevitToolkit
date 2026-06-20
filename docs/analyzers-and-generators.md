# Analyzers & Source Generators

The package bundles Roslyn tooling alongside the runtime library: source generators that emit add-in boilerplate, analyzers that enforce their prerequisites, and code fixers that resolve the diagnostics. This tooling is part of the public contract, so its generated API shape and diagnostic ids are stable surface. This guide is for authoring and extending the tooling.

## Projects

* The source-generator project holds the incremental generators.
* The analyzer project holds the diagnostic analyzers and their `DiagnosticDescriptors` catalog.
* The code-fixer project holds the fixers that resolve the diagnostics.
* The `*.Roslyn###` twins are covered under Roslyn version targeting below.

All target `netstandard2.0` with `IsRoslynComponent=true` and `EnforceExtendedAnalyzerRules=true`, configured in `Directory.Roslyn.props`.

## Generators

A generator turns an annotated partial-type member into generated code, selecting the output shape from the member signature. The trigger attribute's XML docs are the consumer-facing contract and the source of truth for the signature-to-output matrix and its options. Keep that contract in the attribute docs, not duplicated here.

When you author or extend a generator, keep it incremental, emit into a stable namespace, and treat every generated member name as public surface. See [Backward Compatibility](./backward-compatibility.md).

## Diagnostics

Diagnostics use the `RVTTK####` id prefix and live in the `DiagnosticDescriptors` catalog, the source of truth for the current set. They enforce the generators' prerequisites, such as the declaration and signature constraints on an annotated member.

When you add or change a rule:

* Assign the next unused `RVTTK####` id, and never reuse a retired one.
* Add a `DiagnosticDescriptor` with a clear title, a parameterized message format, and an appropriate category and severity.
* Record it in the analyzer's `AnalyzerReleases.Unshipped.md`.
* Add a code fixer when the violation is mechanically resolvable, and cover both in the test projects. See [Testing](./testing.md).

## Roslyn Version Targeting

Each tooling project has a `.Roslyn###` twin. `Directory.Roslyn.props` parses the version suffix, links the base project's source into the twin, and sets `ROSLYN*_OR_GREATER` constants.

* Author code once in the base project. The twin compiles the same source against the older Roslyn.
* Guard any API that differs across Roslyn versions with the `ROSLYN*_OR_GREATER` constants.
* Do not duplicate source into a twin. It is intentionally empty except for its project file.

## Packaging

The runtime project packs each tooling assembly into the version-specific Roslyn analyzer paths so the matching build loads per IDE and SDK. It references the tooling projects with `ReferenceOutputAssembly="false"` purely to enforce build order. See [Package Management](./package-management.md).
