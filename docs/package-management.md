# Package Management

The solution uses centralized NuGet package management. All versions live in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`, with floating and transitive pinning enabled).

## Rules

* Define every package version in `Directory.Packages.props`. Do not add `<Version>` to individual `PackageReference` items.
* Keep Revit-version-specific packages conditional on `$(RevitVersion)`. The Revit API packages (`Nice3point.Revit.Api.*`) float to `$(RevitVersion).*`; per-version packages (`Nice3point.Revit.Extensions`, `Nice3point.TUnit.Revit`) are pinned per Revit version with `Condition="$(RevitVersion) == '20NN'"`.
* Keep shared dependency versions (build, testing, Roslyn) unconditional unless they truly vary by Revit version.
* Use `GlobalPackageReference` only for solution-wide packages (currently `Polyfill` and `JetBrains.Annotations.Sources`).
* Revit API references in the runtime library are `PrivateAssets="all"` — build-time only, never flowing to consumers of the package.

## Dependency Groups

* **Revit API:** `Nice3point.Revit.Api.*` — floating, version-conditional.
* **Build automation:** `ModularPipelines*`, `Sourcy.DotNet`, `Microsoft.VisualStudio.SolutionPersistence` (the `/build` project).
* **Testing:** `TUnit`, `Shouldly`, `BenchmarkDotNet`, plus the Roslyn testing packages (`Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`, `...CodeFix.Testing`).
* **Roslyn:** `Microsoft.CodeAnalysis.CSharp` and `...Workspaces` for the analyzers/fixers/generator.

## Adding Dependencies

1. Add the package version to `Directory.Packages.props`.
2. Add a versionless `PackageReference` to the project that uses it.
3. Keep dependency scope narrow. The shipped runtime library must stay dependency-light — it ships with no runtime package dependencies beyond the bundled analyzers; prefer platform/Revit APIs before introducing a new one.

## Updating Dependencies

* When updating Revit-specific packages, verify every supported Revit version still resolves.
* When updating Roslyn packages, verify both the default and `.Roslyn###` tooling configurations still build. See [Analyzers & Source Generators](./analyzers-and-generators.md).
* Keep dependency updates focused and easy to review; do not mix them with feature work.
* Run the relevant build/tests after any dependency change.
* Renovate (`renovate.json`) manages routine version bumps.
