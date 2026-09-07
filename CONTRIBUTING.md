# Contributing to Nice3point.Revit.Toolkit

Thanks for taking the time to contribute. This guide covers building, testing, issues, and pull requests. For the architecture and conventions, see the project guidelines in [AGENTS.md](AGENTS.md).

## Prerequisites

Before you build the project, install the .NET SDK specified in [global.json](global.json) and [JetBrains Rider](https://www.jetbrains.com/rider/) or [Visual Studio](https://visualstudio.microsoft.com/).
Tests that run inside Revit also require a matching licensed Revit installation.
The generator, analyzer, and code fixer tests run without Revit.

## Issues

* Search the existing issues and discussions before you open a new one.
* For a bug, describe what you expected, what happened, and the smallest steps that reproduce it. Include the Revit version and the package version. For an analyzer or generator report, include the diagnostic id.
* For a feature, describe the problem it solves, not only the solution you have in mind.
* For a large or breaking change, open an issue first so the approach is agreed before you write code.

## Pull Requests

* Keep each pull request focused on one concern. Split unrelated changes into separate pull requests.
* Fork the repository, branch from the default branch, and open a draft pull request early.
* Match the style and patterns of the surrounding code.
* Add or update tests for the behavior you change.
* Verify the change on every Revit version it affects when the Revit API differs between versions.
* Never break an existing public API. Deprecate it instead. The generated API shape and the diagnostic ids are public surface too.
* Update the README, the CHANGELOG, and the XML docs in the same pull request as any public-facing change.
* Write a clear title and description, and link the issue the pull request resolves.
* Make sure the build and the tests pass before you mark the pull request ready for review.

Please keep issues and pull requests respectful and on topic.

## Building

1. Open `Nice3point.Revit.Toolkit.slnx` in your IDE.
2. Select `Release.R27` or `Debug.R27` in the solution configuration menu. The `R27` suffix selects Revit 2027.
3. Build the solution.

From a terminal in the repository root:

```shell
dotnet build -c Release.R27
```

To run the ModularPipelines build, open a terminal in the `build` directory and run:

```shell
dotnet run
```

### Compiler compatibility

The tooling assemblies compile against Roslyn 5.0 and 4.14 and ship in the corresponding `analyzers/dotnet/roslyn*` folders.
These versions are minimum compiler-host requirements, independent of the Revit version and the consumer's target framework.

Keep the Roslyn package references at the minimum supported version for each assembly.
An upgrade to `Microsoft.CodeAnalysis.CSharp` or `Microsoft.CodeAnalysis.CSharp.Workspaces` can raise that requirement even when the source uses no new APIs.
Upgrade the shipped references when a tooling feature requires newer Roslyn APIs or the supported compiler baseline changes.
Keep the CSharp and Workspaces references aligned within each variant.
SDK and IDE upgrades do not require the shipped references to change.

The SDK selects the highest packaged Roslyn version that does not exceed the compiler host version.
A host newer than 5.0 can load the current 5.0 variant.
Keep an older variant when a new baseline would otherwise exclude supported hosts or remove behavior those hosts currently receive.
Create variants at these compatibility boundaries, rather than for every Roslyn minor release.
Each package folder must match its assembly's minimum host version; a 5.9 assembly in the `roslyn5.0` folder is incompatible with 5.0 hosts.

Before upgrading, review [Directory.Roslyn.props](Directory.Roslyn.props), the package paths in the [runtime project](source/Nice3point.Revit.Toolkit/Nice3point.Revit.Toolkit.csproj), and the compiler versions that must remain supported.
Verify loading and generated-source compilation on the minimum host for every retained variant and on the new host.
For package selection, see the [SDK analyzer resolution](https://github.com/dotnet/sdk/blob/main/src/Tasks/Microsoft.NET.Build.Tasks/ResolvePackageAssets.cs).
For the host-version check, see the [Roslyn analyzer loader](https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/DiagnosticAnalyzer/AnalyzerFileReference.cs).

## Testing

### Running compiler tooling tests

From the repository root:

```shell
dotnet test --solution tests/Tooling.slnf -c Release.R27
```

The solution filter selects the generator, analyzer, and code fixer tests.
These projects use TUnit on .NET 10; `Release.R27` maps to their `Release` configuration.
You can also run individual tests from the Unit Tests window in Rider.

To run one test class:

```shell
dotnet test --project tests/Nice3point.Revit.Toolkit.SourceGenerators.Tests/Nice3point.Revit.Toolkit.SourceGenerators.Tests.csproj -c Release --treenode-filter '/*/*/GeneratorSynchronizationTests/*'
```

The first run may download reference assemblies from NuGet.
Generator tests compile against .NET 8 by default; synchronization tests also use .NET 9 references.
The test process uses Roslyn 5.0. Language-version cases do not replace building the `.Roslyn414` projects.

### Adding generator tests

1. Add a case to the relevant test class in `tests/Nice3point.Revit.Toolkit.SourceGenerators.Tests`.
2. Supply the consumer source and call `GeneratorTest.RunAsync`. Include a call to the generated member when the case checks its signature or accessibility.
3. For generated-source changes, verify `driver.GetRunResult()` with `VerifyTUnit.Verifier.Verify`.
4. For runtime behavior, use `GeneratorTest.ExecuteAsync` and assert the returned values.
5. Run the test and review any snapshot differences.

`GeneratorTest` compiles the input and generated source together and checks diagnostics before comparing snapshots.
The [external-event fixture](tests/Nice3point.Revit.Toolkit.SourceGenerators.Tests/Fixtures/ExternalEvents.cs) supplies the API signatures and invokes delegates synchronously.
It does not execute Revit. Keep the fixture consistent with changes to the runtime event API.

### Reviewing snapshots

Expected output lives in [Snapshots](tests/Nice3point.Revit.Toolkit.SourceGenerators.Tests/Snapshots) as `.verified.cs` and `.verified.txt` files.
When output differs, Verify writes a `.received.*` file beside the expected file.

1. Open the difference in your diff tool.
2. Check the complete generated source or diagnostic. For a diagnostic, review its ID, severity, message, and source location.
3. Accept the received output only when it matches the intended behavior.
4. Run the test again and commit the `.verified.*` files with the change.

For diff-tool and Rider integration, see [Verify snapshot management](https://github.com/VerifyTests/Verify#snapshot-management).

### Adding analyzer and code fixer tests

Use `AnalyzerTest` or `CodeFixTest` in `tests/Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests`.
Follow an existing test to mark diagnostic locations with `{|#0:Name|}`, provide the diagnostic arguments, and specify the complete fixed source.
For a code fixer, cover individual application, Fix All, and cases where no safe fix can be offered.
Check that comments and directives survive the edit.

### Running Revit tests

Select a configuration matching your installed Revit version and run the tests from `Nice3point.Revit.Toolkit.Tests` in the IDE.
These tests execute inside Revit and use the Revit test fixture.
Run the compiler tooling tests separately when your change affects a generator, analyzer, or code fixer.

