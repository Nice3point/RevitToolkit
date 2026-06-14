# Testing Strategy

Tests cover the toolkit's **custom logic**, not the Revit API itself. There are two distinct test surfaces: runtime types that run inside a real Revit process, and the Roslyn tooling that runs against the compiler.

## What to Test

* **Custom logic only.** Test types that add behavior — option callbacks, context scopes, handler queuing, generator output.
    * Test: `DuplicateTypeNamesHandler`, `FamilyLoadOptions`, `RevitApiContext` scopes — custom logic.
    * Skip: thin pass-through wrappers that only forward to a Revit API call.
* **Edge cases:** null inputs, empty collections, boundary values.
* **Thread safety & scope nesting:** test concurrent access and nested `Begin...Scope()` usage where applicable.
* **No UI tests:** skip ribbon/dockable-pane and other interactive UI paths.

## Revit-Thread Tests

* **Framework:** TUnit with **Shouldly** assertions; **Nice3point.TUnit.Revit** for Revit API access.
* **Location:** `tests/Nice3point.Revit.Toolkit.Tests`.
* **Execution:** tests run inside the Revit process. The assembly applies `[assembly: TestExecutor<RevitThreadExecutor>]` (`TestsConfiguration.cs`); individual Revit-thread hooks use `[HookExecutor<RevitThreadExecutor>]`.

### Sample-Driven Context

Inherit a shared sample base under `tests/Nice3point.Revit.Toolkit.Tests/Abstractions` (`RevitApiTest` from Nice3point.TUnit.Revit):

* `RevitModelSampleTest` — opens `.rvt` sample models.
* `RevitFamilySampleTest` — opens `.rfa` sample families.

Sample files resolve from the installed Revit `Samples` folder via `RevitEnvironment.MajorVersion`. Each document is copied to a temp path, opened under `RevitApiContext.BeginFailureSuppressionScope()`, and closed/deleted in the matching teardown hook. When the `Samples` folder is missing, the sample arrays are empty — guard tests so they skip cleanly rather than fail.

```csharp
public class MyFeatureTests : RevitModelSampleTest
{
    [Test]
    [HookExecutor<RevitThreadExecutor>]
    public async Task MyFeature_ValidModel_ReturnsExpectedResult()
    {
        foreach (var document in ModelDocuments.Values)
        {
            var result = MyFeature.Run(document);
            await Assert.That(result).IsNotNull();
        }
    }
}
```

## Analyzer, Code-Fixer & Generator Tests

These run against the compiler, not Revit:

* **Analyzers & code fixers:** `tests/Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests` using `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` / `CodeFix.Testing` via the `CSharpCodeFixVerifier` helpers under `Verifiers/`. Assert both the reported diagnostic (`RVTTK####`) and the fixed source.
* **Source generator:** `tests/Nice3point.Revit.Toolkit.SourceGenerators.Tests` using `GeneratorTestHelper` to drive the generator and verify the emitted source and any reported diagnostics.

See [Analyzers & Source Generators](./analyzers-and-generators.md) for the diagnostic catalog and Roslyn-version targeting.

## Version Coverage

* Tests build per Revit configuration (`Debug.RNN`). Prefer the latest supported debug configuration unless the change is version-specific.
* When changing version-specific behavior, run or document coverage for each affected `Debug.RNN` configuration the project declares.
