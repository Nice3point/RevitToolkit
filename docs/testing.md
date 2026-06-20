# Testing

Tests cover the toolkit's custom logic, not the Revit API itself. Every change ships with tests. There are two test surfaces: runtime types that run inside a real Revit process, and the Roslyn tooling that runs against the compiler.

## What to Test

* **Custom logic only.** Test types that add behavior: option callbacks, context scopes, handler queuing, generator output. Test `DuplicateTypeNamesHandler`, `FamilyLoadOptions`, and `RevitApiContext` scopes. Skip a thin pass-through wrapper that only forwards to a Revit API call.
* **Edge cases:** null inputs, empty collections, boundary values.
* **Thread safety and scope nesting:** test concurrent access and nested `Begin...Scope()` usage where applicable.
* **No UI tests.** Skip ribbon, dockable-pane, and other interactive paths.

## Revit-Thread Tests

* **Framework:** TUnit on the Microsoft.Testing.Platform, with Nice3point.TUnit.Revit for Revit API access. Assertions use the TUnit API: `await Assert.That(actual).IsNotNull()`.
* **Location:** `tests/Nice3point.Revit.Toolkit.Tests`.
* **Execution:** tests run inside the Revit process. The assembly applies `[assembly: TestExecutor<RevitThreadExecutor>]` (`TestsConfiguration.cs`). Individual Revit-thread hooks use `[HookExecutor<RevitThreadExecutor>]`.
* **Structure:** split each test into blocks marked with `// Arrange`, `// Act`, and `// Assert` comments.

### Sample-Driven Context

Inherit a shared sample base under `tests/Nice3point.Revit.Toolkit.Tests/Abstractions` (`RevitApiTest` from Nice3point.TUnit.Revit):

* `RevitModelSampleTest` opens `.rvt` sample models.
* `RevitFamilySampleTest` opens `.rfa` sample families.

Sample files resolve from the installed Revit `Samples` folder via `RevitEnvironment.MajorVersion`. Each document is copied to a temp path, opened under `RevitApiContext.BeginFailureSuppressionScope()`, and closed and deleted in the matching teardown hook. When the `Samples` folder is missing, the sample arrays are empty, so guard tests to skip cleanly rather than fail.

```csharp
public class MyFeatureTests : RevitModelSampleTest
{
    [Test]
    [HookExecutor<RevitThreadExecutor>]
    public async Task MyFeature_ValidModel_ReturnsExpectedResult()
    {
        foreach (var document in ModelDocuments.Values)
        {
            // Arrange
            var feature = new MyFeature(document);

            // Act
            var result = feature.Run();

            // Assert
            await Assert.That(result).IsNotNull();
        }
    }
}
```

## Analyzer, Code-Fixer, and Generator Tests

These run against the compiler, not Revit:

* **Analyzers and code fixers:** `tests/Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests` using `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` and `CodeFix.Testing` through the `CSharpCodeFixVerifier` helpers under `Verifiers/`. Assert both the reported diagnostic (`RVTTK####`) and the fixed source.
* **Source generator:** `tests/Nice3point.Revit.Toolkit.SourceGenerators.Tests` using `GeneratorTestHelper` to drive the generator and verify the emitted source and any reported diagnostics.

See [Analyzers & Source Generators](./analyzers-and-generators.md) for the diagnostic catalog and Roslyn-version targeting.

## Version Coverage

* Tests build per Revit configuration (`Debug.RNN`). Prefer the latest supported debug configuration unless the change is version-specific.
* When changing version-specific behavior, run or document coverage for each affected `Debug.RNN` configuration the project declares.

## Build and Test

TUnit runs on the Microsoft.Testing.Platform, so `dotnet test` runs the suite directly. Pass the target Revit configuration, for example `dotnet test -c Debug.R27`.
