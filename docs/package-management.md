# Package Management

The solution uses centralized NuGet package management. All versions live in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`, with floating and transitive pinning enabled). Renovate (`renovate.json`) bumps versions automatically, so manual version edits are rare.

## Rules

* Define every package version in `Directory.Packages.props`. Do not add `<Version>` to individual `PackageReference` items.
* Keep Revit-version-specific packages conditional on `$(RevitVersion)`. The Revit API packages float to `$(RevitVersion).*`, and the per-version packages are pinned with a `$(RevitVersion)` condition.
* Keep shared dependency versions unconditional unless they truly vary by Revit version.
* Use `GlobalPackageReference` only for solution-wide packages such as the polyfill and annotation sources.
* Revit API references in the runtime library are `PrivateAssets="all"`, so they stay build-time only and never flow to consumers of the package.

## Add a Dependency

1. Add the package version to `Directory.Packages.props`.
2. Add a versionless `PackageReference` to the project that uses it.
3. Keep the scope narrow. The shipped runtime library stays dependency-light. It ships with no runtime package dependencies beyond the bundled analyzers, so prefer platform or Revit APIs before introducing a new one.
