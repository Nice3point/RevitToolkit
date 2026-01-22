# RevitToolkit Guidelines

## 1. Project Structure

### 1.1. Solution Organization

* **`/source`**: Core library project.
    * `Nice3point.Revit.Toolkit`: Toolkit classes for Revit API (contexts, external commands, handlers, options, helpers).
* **`/tests`**: Testing projects.
    * `Nice3point.Revit.Toolkit.Tests`: Unit tests executed in Revit context using Nice3point.TUnit.Revit.
* **Root Level**:
    * Configuration files: `Directory.Build.props`, `Directory.Packages.props`
    * Documentation: `Readme.md`, `Changelog.md`
    * CI/CD: `.github/workflows`

## 2. Architecture Principles

### 2.1. Core Design Goals

* **Static Context Access:** Provide global access to Revit Application and UI contexts.
* **Simplification:** Abstract boilerplate code for Revit interfaces (`IExternalCommand`, `IExternalApplication`, etc.).
* **Async Support:** Enable async/await patterns within Revit external commands and event handlers.
* **Thread Safety:** Ensure thread-safe operations for shared state.
* **Disposable Scopes:** Use `IDisposable` pattern for resource management.
* **Analyzers:** Use JetBrains.Annotations for static code analysis.
* **Backward Compatibility:** Never break existing public APIs.

## 3. Strict C# Production Style

### 3.1. General Principles

* **Instance Classes:** Use instance-based classes with inheritance for external commands/applications.
* **Static Context Classes:** Use static classes for global context access.
* **Disposable Scope Pattern:** Return `IDisposable` for scoped operations.
* **Pure Functions:** Mark read-only operations with `[Pure]` attribute where applicable.
* **Explicit over Implicit:** Code should be self-explanatory.

### 3.2. Naming Conventions

* **Clarity is King:** Names must be descriptive.
* **Revit API Patterns:** Follow Revit API naming conventions.
* **Scope Methods:** Use `Begin...Scope` pattern for disposable scopes:
    * ✅ `BeginFailureSuppressionScope()`, `BeginDialogSuppressionScope()`
    * ❌ `SuppressFailures()`, `SuppressDialogs()`
* **No Abbreviations:**
    * ❌ `elem`, `doc`, `param`, `app`
    * ✅ `element`, `document`, `parameter`, `application`

### 3.3. Class Structure

* **File-Scoped Namespaces:** Always use `namespace Nice3point.Revit.Toolkit;` or sub-namespaces.
* **PublicAPI Attribute:** Mark all public classes with `[PublicAPI]`.
* **EditorBrowsable:** Mark callback methods with `[EditorBrowsable(EditorBrowsableState.Never)]`.
* **XML Documentation:** Document all public members with `<summary>`, `<param>`, `<returns>`, `<remarks>`, `<example>` blocks.

### 3.4. Disposable Scope Pattern

All scoped operations should return `IDisposable` for automatic resource management:

```csharp
public static IDisposable BeginSomeScope()
{
    lock (SomeLock)
    {
        if (_scopeCount++ == 0)
        {
            // Subscribe to events or acquire resources
        }
    }

    return new SomeScope();
}

private sealed class SomeScope : IDisposable
{
    private int _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        lock (SomeLock)
        {
            if (--_scopeCount == 0)
            {
                // Unsubscribe from events or release resources
            }
        }
    }
}
```

### 3.5. External Command/Application Pattern

Base classes should handle boilerplate and provide simplified override methods:

```csharp
public abstract class SomeExternalCommand : IExternalCommand
{
    public Result Result { get; set; } = Result.Succeeded;
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // Setup and assembly resolution
        using (ResolveHelper.BeginAssemblyResolveScope(GetType()))
        {
            Execute();
        }
        return Result;
    }

    public abstract void Execute();
}
```

### 3.6. Async Command Pattern

Use `DispatcherFrame` for message pumping in async commands:

```csharp
public sealed override void Execute()
{
    var task = ExecuteAsync();
    if (task.IsCompleted)
    {
        task.GetAwaiter().GetResult();
        return;
    }

    var frame = new DispatcherFrame();
    task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
    Dispatcher.PushFrame(frame);
    task.GetAwaiter().GetResult();
}
```

### 3.7. Event Handler Pattern

Use `ConcurrentQueue` for thread-safe action queuing:

```csharp
private readonly ConcurrentQueue<Action<UIApplication>> _queue = new();

public void Raise(Action<UIApplication> action)
{
    if (RevitContext.IsRevitInApiMode)
    {
        action(RevitContext.UiApplication);
        return;
    }

    _queue.Enqueue(action);
    Raise();
}
```

### 3.8. Error Handling

* **Revit Exceptions:** Document Revit API exceptions in XML comments.
* **No Swallowing:** Let Revit exceptions propagate to caller by default.
* **Optional Exception Handler:** Provide `SetExceptionHandler()` for handlers that process multiple actions.
* **Validation:** Use `ThrowWhen()` pattern for internal validation.

### 3.9. Thread Safety

* **Lock Objects:** Use `Lock` class for synchronization.
* **Interlocked Operations:** Use `Interlocked.Exchange` for dispose flags.
* **Scope Counting:** Track nested scopes with counters under lock.

### 3.10. Compilation Directives

* **Revit Version Support:** Use `#if REVIT2024_OR_GREATER` for version-specific APIs.
* **.NET Version Support:** Use `#if NET8_0_OR_GREATER` for runtime-specific features (e.g., `UnsafeAccessor`).
* **Consistent Patterns:** Apply directives consistently across related methods.

## 4. Backward Compatibility

### 4.1. Obsolete Attribute Pattern

**NEVER** delete existing public APIs. Mark them as obsolete instead:

```csharp
[Obsolete("Use NewClass.NewMethod instead")]
[CodeTemplate(
    searchTemplate: "OldClass.OldMethod",
    Message = "OldClass.OldMethod is obsolete, use NewClass.NewMethod instead",
    ReplaceTemplate = "NewClass.NewMethod",
    ReplaceMessage = "Replace with NewClass.NewMethod")]
public static SomeType OldMethod() { }
```

### 4.2. Obsolete Guidelines

* **Message:** Clear explanation with replacement method/property name.
* **CodeTemplate:** Provide JetBrains ReSharper auto-conversion pattern.
* **Implementation:** Obsolete member must call **original implementation**, not the new method.
* **Class-Level Obsolete:** Mark entire obsolete classes with `[Obsolete]` attribute.

### 4.3. Breaking Changes

* **Method Signature:** Never change existing method signatures.
* **Return Type:** Never change return types.
* **Parameters:** Add optional parameters only at the end.
* **Renaming:** Use Obsolete pattern, keep old member functional.
* **Class Replacement:** Create new class, mark old as obsolete.

## 5. Documentation Requirements

### 5.1. Readme.md

* **Add Examples:** Every new feature must have usage examples.
* **Code Blocks:** Use proper C# syntax highlighting.
* **Complete Coverage:** Document all public classes and their primary use cases.

### 5.2. Changelog.md

* **Version Sections:** Update the current preview/release version section.
* **Categories:**
    * **New Features:** New classes, methods, overloads.
    * **Breaking Changes:** Renamed methods, changed behavior.
    * **Improvements:** Performance, refactoring.
    * **Bug Fixes:** Corrections to existing functionality.
* **Migration Examples:** Show at the end for major changes.
* **Complete Documentation:** Document ALL changes, not just major ones.

### 5.3. XML Documentation

* **Summary:** Describe what the member does.
* **Parameters:** Document each parameter with context.
* **Returns:** Describe return value meaning.
* **Remarks:** Add implementation details, constraints, thread-safety notes.
* **Example:** Provide usage examples for complex APIs.
* **Exceptions:** Document all possible Revit API exceptions.

## 6. Testing Strategy

### 6.1. Test Scope

* **Custom Logic Only:** Test classes with user-defined logic, NOT simple wrappers.

### 6.2. Test Framework

* **Framework:** TUnit with Nice3point.TUnit.Revit for Revit context execution.
* **Location:** `tests/Nice3point.Revit.Toolkit.Tests`.
* **Execution:** Tests run inside Revit process using `[TestExecutor<RevitThreadExecutor>]`.

### 6.3. Test Data Pattern

Use `MethodDataSource` to test against all Revit sample files:

```csharp
private static readonly string SamplesPath = $@"C:\Program Files\Autodesk\Revit {Application.VersionNumber}\Samples";

[Before(Class)]
public static void ValidateSamples()
{
    if (!Directory.Exists(SamplesPath))
    {
        Skip.Test($"Samples folder not found at {SamplesPath}");
        return;
    }

    if (!Directory.EnumerateFiles(SamplesPath, "*.rvt").Any())
    {
        Skip.Test($"No .rvt files found in {SamplesPath}");
    }
}

public static IEnumerable<string> GetSampleFiles()
{
    if (!Directory.Exists(SamplesPath))
    {
        yield return string.Empty;
        yield break;
    }

    foreach (var file in Directory.EnumerateFiles(SamplesPath, "*.rvt")) yield return file;
}

[Test]
[TestExecutor<RevitThreadExecutor>]
[MethodDataSource(nameof(GetSampleFiles))]
public async Task MyFeature_ValidFile_ReturnsExpectedResult(string filePath)
{
    Document? document = null;

    try
    {
        document = Application.OpenDocumentFile(filePath);

        // Test logic here

        await Assert.That(result).IsNotNull();
    }
    finally
    {
        document?.Close(false);
    }
}
```

### 6.4. Test Coverage

* **Edge Cases:** Null inputs, empty collections, boundary values.
* **Thread Safety:** Test concurrent access where applicable.
* **Scope Nesting:** Test nested disposable scopes.
* **Revit API Constraints:** Test against actual Revit objects, not mocks.

## 7. Performance Guidelines

### 7.1. Revit API Optimization

* **Batch Operations:** Prefer batch APIs over individual calls.
* **Transactions:** Minimize transaction scope.
* **Event Handlers:** Unsubscribe when scope ends.

### 7.2. Memory Allocation

* **Avoid LINQ:** For hot paths, use traditional loops instead of LINQ.
* **Collection Sizing:** Pre-allocate collections when size is known.
* **ConcurrentQueue:** Use for thread-safe queuing in handlers.

### 7.3. Thread Safety

* **Lock Granularity:** Keep lock sections minimal.
* **Avoid Deadlocks:** Use `TaskScheduler.Default` for continuations.
* **Interlocked:** Prefer for simple flag operations.

## 8. Package Management

* **Centralized:** All versions are defined in `Directory.Packages.props`.
* **Multi-targeting:** Support Revit 2019-2026 configurations (Debug.R19-R26, Release.R19-R26).
* **Dependencies:**
    * `JetBrains.Annotations` - Code analysis attributes
* **Conditional Compilation:**
    * `#if REVIT2024_OR_GREATER` for Revit API changes
    * `#if NET8_0_OR_GREATER` for .NET runtime features