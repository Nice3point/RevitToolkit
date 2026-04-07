# 2027.0.0

### External Events

New family of external event types replacing legacy `ActionEventHandler`, `AsyncEventHandler`:

- **ExternalEvent** — synchronous external event that queues the handler via the Revit external event mechanism.
- **ExternalEvent\<T>** — generic synchronous external event that accepts an argument of type `T`.
- **AsyncExternalEvent** — asynchronous external event with `RaiseAsync()` that returns a `Task`.
- **AsyncExternalEvent\<T>** — generic asynchronous external event with argument support.
- **AsyncRequestExternalEvent\<TResult>** — asynchronous external event that returns a result via `RaiseAsync()`.
- **AsyncRequestExternalEvent\<T, TResult>** — generic asynchronous external event with argument and result support.
- **ExternalEventOptions** — configuration flags for event behavior, including `AllowDirectInvocation` for direct execution in API context.

> [!IMPORTANT]
> Events do not need to be created inside the Revit API context — the Toolkit handles initialization automatically, so you can create them anywhere in your code and on any thread.

### ExternalEvent Source Generator

New `[ExternalEvent]` attribute with incremental source generator that eliminates boilerplate for defining external events.
Annotate a method in a partial type and the generator produces typed event properties automatically:

```c#
public partial class MyViewModel : ObservableObject
{
    [ExternalEvent]
    private void DeleteWindows(UIApplication application)
    {
        var document = application.ActiveUIDocument.Document;
        using var transaction = new Transaction(document, "Delete windows");
        transaction.Start();
        document.Delete(document.GetInstanceIds(BuiltInCategory.OST_Windows));
        transaction.Commit();
    }
    
    [RelayCommand]
    private void DeleteWindows()
    {
        DeleteWindowsEvent.Raise();
    }
}

// Generates:
// public IExternalEvent DeleteWindowsEvent => field ??= new ExternalEvent(DeleteWindows);
// public IAsyncExternalEvent DeleteWindowsAsyncEvent => field ??= new AsyncExternalEvent(DeleteWindows);
```

Source generator supports multiple parameters, methods with extra parameters like `private void DeleteWindows(string arg1, int arg2, bool arg3)` also work.
Roslyn versions 4.14 and 5.0+ supported.

### Roslyn Analyzers and Code Fixers

New analyzer package with diagnostics for `[ExternalEvent]` annotated methods:

- **RVTTK0001** — Method returns `Task` or `Task<T>`.
- **RVTTK0002** — Method is `async void`.
- **RVTTK0003** — Method is generic.
- **RVTTK0004** — Duplicate method overloads with `[ExternalEvent]`.
- **RVTTK0005** — Containing type is not `partial`.

### Context

- New **RevitContext** class for UI-level application context access.
- New **RevitApiContext** class for database-level application context access.
- New **AsyncExternalCommand** class for async/await support in external commands.
- New **AsyncExternalApplication** class for async/await support in external applications.
- New **BeginDialogSuppressionScope()** method with disposable pattern for dialog suppression.
- New **BeginFailureSuppressionScope()** method with disposable pattern for failure handling.
- New **BeginAssemblyResolveScope()** method with disposable pattern for dependency resolution.
- New **BeginAssemblyResolveScope(string directory)** overload for explicit path specification.`
- New overloads for `BeginDialogSuppressionScope()`: `MessageBoxResult`, `TaskDialogResult`, custom handler.

## Improvements

- Improve reflexion performance with `UnsafeAccessor` methods for .NET 8+.
- Disposable scopes support nesting with reference counting.
- Thread safety improvements with `Lock` class and `Interlocked` operations.
- `BeginAssemblyResolveScope` now uses Stack to support nested scopes with different directories.

## Breaking Changes

- **Context** class is now obsolete, use `RevitContext` or `RevitApiContext` instead.
- **SuppressDialogs()** / **RestoreDialogs()** are obsolete, use `BeginDialogSuppressionScope()` instead.
- **SuppressFailures()** / **RestoreFailures()** are obsolete, use `BeginFailureSuppressionScope()` instead.
- **BeginAssemblyResolve()** / **EndAssemblyResolve()** are obsolete, use `BeginAssemblyResolveScope()` instead.
- **ExternalCommand.UiApplication** is obsolete, use `Application` instead.
- **ExternalCommand.UiDocument** is obsolete, use `Application.ActiveUIDocument` instead.
- **ExternalCommand.Document** is obsolete, use `Application.ActiveUIDocument.Document` instead.
- **ExternalApplication.UiApplication** is obsolete, use `RevitContext.UiApplication` instead.
- **ActionEventHandler** is now obsolete — use `ExternalEvent` or `[ExternalEvent]` source generator.
- **AsyncEventHandler** is now obsolete — use `AsyncExternalEvent` or `[ExternalEvent]` source generator.
- **AsyncEventHandler\<T>** is now obsolete — use `AsyncRequestExternalEvent<T>` or `[ExternalEvent]` source generator.
- **IdlingEventHandler** is now obsolete — use `ExternalEvent` or `[ExternalEvent]` source generator.

## Automatic Migration with ReSharper/Rider

All obsolete methods are marked with `[CodeTemplate]` attributes, enabling automatic code conversion in JetBrains ReSharper and Rider. Simply place your cursor on the obsolete method and use the suggested quick-fix to update to the new API.

## Migration Guide

Replace `Context` with `RevitContext` or `RevitApiContext`:

```csharp
// Before
Context.ActiveDocument.Delete(elementId);
Context.Application.Username;

// After (auto-fix available)
RevitContext.ActiveDocument.Delete(elementId);
RevitApiContext.Application.Username;
```

Replace manual suppress/restore with disposable scopes:

```csharp
// Before
try
{
    Context.SuppressDialogs();
    Context.SuppressFailures();
    // operations
}
finally
{
    Context.RestoreDialogs();
    Context.RestoreFailures();
}

// After (auto-fix available)
using (RevitContext.BeginDialogSuppressionScope())
using (RevitApiContext.BeginFailureSuppressionScope())
{
    // operations
}
```

Replace `BeginAssemblyResolve`/`EndAssemblyResolve` with scope:

```csharp
// Before
try
{
    ResolveHelper.BeginAssemblyResolve<MyType>();
    window.Show();
}
finally
{
    ResolveHelper.EndAssemblyResolve();
}

// After (auto-fix available)
using (ResolveHelper.BeginAssemblyResolveScope<MyType>())
{
    window.Show();
}
```

New: Specify directory path directly for assembly resolution:

```csharp
// Path-based (new)
using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Libraries"))
{
    window.Show();
}

// Nested scopes (new) - searches innermost first
using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Shared\Common"))
using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Plugin"))
using (ResolveHelper.BeginAssemblyResolveScope<MyType>())
{
    // Searches: MyType directory -> Plugin -> Common
    window.Show();
}
```

Replace legacy event handlers with new External Events:

```csharp
// Before
private readonly ActionEventHandler _handler = new();

private void Execute()
{
    _handler.Raise(app =>
    {
        app.ActiveUIDocument.Document.Delete(elementId);
    });
}

// After
private readonly ExternalEvent _handler = new(app =>
{
    app.ActiveUIDocument.Document.Delete(elementId);
});

private void Execute()
{
    _handler.Raise();
}
```

Replace legacy async event handlers:

```csharp
// Before
private readonly AsyncEventHandler _handler = new();

private async Task ExecuteAsync()
{
    await _handler.RaiseAsync(app =>
    {
        app.ActiveUIDocument.Document.Delete(elementId);
    });
}

// After
private readonly AsyncExternalEvent _handler = new(app =>
{
    app.ActiveUIDocument.Document.Delete(elementId);
});

private async Task ExecuteAsync()
{
    await _handler.RaiseAsync();
}
```

Or use the source generator to avoid boilerplate entirely:

```csharp
// After (source generator)
partial class MyViewModel
{
    [ExternalEvent]
    private void DeleteElement(UIApplication application)
    {
        application.ActiveUIDocument.Document.Delete(elementId);
    }
}

// Usage:
DeleteElementEvent.Raise();
// or
await DeleteElementAsyncEvent.RaiseAsync();
```

# 2026.0.0

- New `Context.UiControlledApplication` property. Helps to manipulate with the Revit ribbon, context menus outside ExternalApplication. 
- Now `AsyncEventHandler{T}` works in a multithreaded application and returns the result to each recipient.
- Removed `AssemblyLoadContext` for addins isolation. It will be moved to Revit itself. [More info.](https://feedback.autodesk.com/project/forum/thread.html?cap=cb0fd5af18bb49b791dfa3f5efc47a72&forid=%7B057e532f-e478-43d9-affc-01b3deb82a76%7D&topid=%7B8C202188-9EA5-49BE-B95F-7F5115507C88%7D)
- Removed deprecated features.
- Fixed dependency resolver to avoid conflict with the Revit runtime.

# 2025.0.3

- Removed JetBrains.Annotations dependency
- Fixed typo in summary

# 2025.0.2

- **Context** global handlers:
    - **SuppressFailures**: suppresses the display of the Revit error and warning messages during transaction.
      By default, Revit uses manual error resolution control with user interaction.
      This method provides automatic resolution of all failures without notifying the user or interrupting the program
    - **SuppressDialogs**: suppresses the display of the Revit dialogs
- **Context** global properties:
    - **IsRevitInApiMode**: determines whether Revit is in API mode or not.
    - **Document** is obsolete. Use _ActiveDocument_ instead.
    - **UiDocument** is obsolete. Use _ActiveUiDocument_ instead.
- **ExternalCommand.SuppressFailures** is obsolete. Use Context class instead.
- **ExternalCommand.SuppressDialogs** is obsolete. Use Context class instead.
- **ActionEventHandler** now understands when it is in a Revit context and calls the handler immediately, without adding to the queue.
- **AsyncEventHandler** now understands when it is in a Revit context and calls the handler immediately, without adding to the queue.
  Awaiting with `await` keyword will not cause a context switch, and you can still call API requests in the main Revit thread.
- Nullable types support
- Assembly load context internal optimizations
- Readme updates, include extra samples

# 2025.0.1

## Add-in dependencies isolation

This release introduces an isolated plugin dependency container using .NET **AssemblyLoadContext**.
This feature allows plugins to run in a separate, isolated context, ensuring
independent operation and preventing conflicts from incompatible library versions. 
This enhancement is available for Revit 2025 and higher, addressing the limitations of Revit's traditional plugin loading mechanism, which loads plugins by path without native support for isolation.

![изображение](https://github.com/jeremytammik/RevitLookup/assets/20504884/d1e160a2-36ef-43ad-a384-fdcc15b0106e)

**How It Works:**

The core functionality centers on **AssemblyLoadContext**, which creates an isolated container for each plugin. 
When a plugin is loaded, it is assigned a unique **AssemblyLoadContext** instance, encapsulating the plugin and its dependencies to prevent interference with other plugins or the main application. 

To use this isolation feature, developers must inherit their classes from:
- ExternalCommand
- ExternalApplication
- ExternalDbApplication
- ExternalCommandAvailability

These classes contain the built-in isolation mechanism under the hood.
Plugins using interfaces such as **IExternalCommand** will not benefit from this isolation and will run in the default context.

**Limitations:**

- The isolated plugin context feature is available starting with Revit 2025. 
- For older Revit versions, this library uses a **ResolveHelper** to help load dependencies from the plugin's folder, but does not protect against conflicts arising from incompatible packages.
- Additionally, plugins that do not inherit from the specified classes will not be isolated and may experience compatibility issues if they rely on the default context.

## Improvements

- Added **ExternalCommandAvailability** class.

    It involves isolating dependencies.
    If your implementation does not include dependencies, use the IExternalCommandAvailability interface to reduce memory allocation
- Added **AvailableCommandController** class. 

    ExternalCommandAvailability implementation. 
    Controller providing permanent accessibility for ExternalCommand invocation. Usage:
    ```C#
    panel.AddPushButton<StartupCommand>("Execute")
        .SetAvailabilityController<AvailableCommandController>()
    ```

# 2025.0.0

- Revit 2025 support
- Add new DuplicateTypeNamesHandler overload
- Optimize assembly resolve by @dosymep
- Fix resolve raw assembly by @dosymep

# 2024.0.1

- New **Context** class.

    Provides computed properties to retrieve Revit objects in the current session. Values are provided even outside the Revit context.

    - Context.UiApplication;
    - Context.Application;
    - Context.UiDocument;
    - Context.Document;
    - Context.ActiveView;
    - Context.ActiveGraphicalView;
    - Context.ActiveView;

    ```C#
    Context.Document.Create.NewFamilyInstance();
    Context.ActiveView = view;
    ```

- New **FrameworkElementCreator**. Creates FrameworkElements for the dock pane.

    ```C#
    DockablePaneProvider.Register(application)
        .SetId(guid)
        .SetTitle(title)
        .SetConfiguration(data =>
        {
            data.FrameworkElementCreator = new FrameworkElementCreator<DockPaneView>();
        });
    ```
  Available overloading with IServiceProvider, in case you use hosting.

- **ResolveHelper** reworked. Now you need to specify a type to resolve dependencies. The directory where the type is defined will be used to search for dependencies.

  Enabled by default for all ExternalCommand and ExternalApplication, so only needed for direct invocation in special cases.

  The current version now disables all resolvers used in the domain to avoid conflicts, and to bypass cases where Revit loaded assemblies from another plugin's folder.

  ```c#
  try
  {
      ResolveHelper.BeginAssemblyResolve<DockView>();
      window.Show();
  }
  finally
  {
      ResolveHelper.EndAssemblyResolve();
  }
  ```

# 2024.0.0

- Revit 2024 support
- New SaveSharedCoordinatesCallback
- New ExternalDbApplication

ExternalCommand:

- New SuppressFailures method

# 2023.0.12

New runtime attributes: init, required keywords support

# 2023.0.11

- New AsyncEventHandler
- New AsyncEventHandler<T>
- New ResolveHelper.ResolveAssembly
- New FamilyLoadOptions overloads

- Updated ExternalCommand
- Updated ExternalApplication

- Fixed ExternalEventHandler performance

- Removed ActionEventHandler params versions. Use parameterless version
- Removed IdlingEventHandler params versions. Use parameterless version

# 2023.0.10

Dependency resolver for ExternalApplication

Nuget symbol server support: https://symbols.nuget.org/download/symbols

# 2023.0.9

TransactionUtils:

- Updated transaction mechanism
- New Modify document method

# 2023.0.8

Fixed security issues

# 2023.0.7

Fixed dependency search issue for several plugins loaded in Revit

# 2023.0.6

- External command minor backend optimization

# 2023.0.5

External command:

- New SuppressExceptions() method
- New SuppressDialogs() method
- New ActiveView property
- New Application property
- Updated Assembly resolver
- Removed ExceptionHandler

# 2023.0.4

- Change namespace for DockablePaneProvider

# 2023.0.3

- New ExternalApplication

# 2023.0.2

- New ExternalCommand

# 2023.0.1

- New DockablePaneProvider
- New SelectionConfiguration

# 2023.0.0

- Initial release