<p align="center">
    <picture>
        <source media="(prefers-color-scheme: dark)" width="610" srcset="https://github.com/Nice3point/RevitToolkit/assets/20504884/852aba24-118f-4908-949d-2e0c019c83da">
        <img alt="RevitLookup" width="610" src="https://github.com/Nice3point/RevitToolkit/assets/20504884/c59042df-b9b5-4829-9417-006912781cf2">
    </picture>
</p>

## Make Revit API more flexible

[![Nuget](https://img.shields.io/nuget/vpre/Nice3point.Revit.Toolkit?style=for-the-badge)](https://www.nuget.org/packages/Nice3point.Revit.Toolkit)
[![Downloads](https://img.shields.io/nuget/dt/Nice3point.Revit.Toolkit?style=for-the-badge)](https://www.nuget.org/packages/Nice3point.Revit.Toolkit)
[![Last Commit](https://img.shields.io/github/last-commit/Nice3point/RevitToolkit/develop?style=for-the-badge)](https://github.com/Nice3point/RevitToolkit/commits/develop)

This library provides a modern interface for working with the Revit API.
Package contains interfaces implementation frequently encountered in revit, aiming to provide as much flexibility as possible, so developers are free to choose which components to
use.

## Installation

You can install the Toolkit as a [NuGet package](https://www.nuget.org/packages/Nice3point.Revit.Toolkit).

The packages are compiled for specific versions of Revit. To support different versions of libraries in one project, use the `RevitVersion` property:

```xml
<PackageReference Include="Nice3point.Revit.Toolkit" Version="$(RevitVersion).*"/>
```

Package included by default in [Revit Templates](https://github.com/Nice3point/RevitTemplates).

## Table of contents

<!-- TOC -->
* [External Commands](#external-commands)
  * [ExternalCommand](#externalcommand)
  * [AsyncExternalCommand](#asyncexternalcommand)
* [External Applications](#external-applications)
  * [ExternalApplication](#externalapplication)
  * [AsyncExternalApplication](#asyncexternalapplication)
  * [ExternalDBApplication](#externaldbapplication)
* [External events](#external-events)
  * [ExternalEvent](#externalevent)
  * [ExternalEvent\<T>](#externaleventt)
  * [AsyncExternalEvent](#asyncexternalevent)
  * [AsyncExternalEvent\<T>](#asyncexternaleventt)
  * [AsyncRequestExternalEvent\<TResult>](#asyncrequestexternaleventtresult)
  * [AsyncRequestExternalEvent\<T, TResult>](#asyncrequestexternaleventt-tresult)
  * [ExternalEventOptions](#externaleventoptions)
  * [ExternalEvent attribute](#externalevent-attribute)
* [Context](#context)
  * [RevitApiContext](#revitapicontext)
  * [RevitContext](#revitcontext)
* [Options](#options)
  * [FamilyLoadOptions](#familyloadoptions)
  * [DuplicateTypeNamesHandler](#duplicatetypenameshandler)
  * [SaveSharedCoordinatesCallback](#savesharedcoordinatescallback)
  * [FrameworkElementCreator](#frameworkelementcreator)
  * [SelectionConfiguration](#selectionconfiguration)
* [Decorators](#decorators)
  * [DockablePaneProvider](#dockablepaneprovider)
* [Helpers](#helpers)
  * [ResolveHelper](#resolvehelper)
<!-- TOC -->

## Features

### External Commands

The Toolkit provides base classes for Revit external commands with implemented:

- Automatic dependency resolution to avoid `FileNotFoundException` exceptions (dependencies are searched in the plugin folder)
- Simplified method signatures — override `Execute()` instead of implementing full interface

#### ExternalCommand

Implementation for **IExternalCommand**. Override `Execute()` to implement a command:

```c#
[Transaction(TransactionMode.Manual)]
public class Command : ExternalCommand
{
    public override void Execute()
    {
        var document = Application.ActiveUIDocument.Document;
    }
}
```

#### AsyncExternalCommand

Implementation for asynchronous **IExternalCommand**. Override `ExecuteAsync()` for async/await support.

The Revit UI remains responsive during async operations through dispatcher message pumping.
Ideal for I/O-bound operations such as HTTP requests, file operations, or database queries.

```c#
[Transaction(TransactionMode.Manual)]
public class Command : AsyncExternalCommand
{
    public override async Task ExecuteAsync()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync("https://example.com");

        using var transaction = new Transaction(Application.ActiveUIDocument.Document, "Update Parameter");
        transaction.Start();

        var element = Application.ActiveUIDocument.Document.GetElement(id);
        element?.FindParameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(response);

        transaction.Commit();
    }
}
```

### External Applications

The Toolkit provides base classes for Revit external applications with implemented:

- Automatic dependency resolution to avoid `FileNotFoundException` exceptions (dependencies are searched in the plugin folder)
- Simplified method signatures — override `OnStartup()`/`OnShutdown()` instead of implementing full interface

#### ExternalApplication

Implementation for **IExternalApplication**. Override `OnStartup()` and optionally `OnShutdown()`:

```c#
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        var panel = Application.CreatePanel("Commands", "RevitAddin");
        panel.AddPushButton<Command>("Execute");
            .SetImage("/RevitAddin;component/Resources/Icons/RibbonIcon16.png");
            .SetLargeImage("/RevitAddin;component/Resources/Icons/RibbonIcon32.png");
    }

    public override void OnShutdown()
    {
    }
}
```

#### AsyncExternalApplication

Implementation for asynchronous **IExternalApplication**. Override `OnStartupAsync()` and optionally `OnShutdownAsync()` for async/await support.

The Revit UI remains responsive during async operations through dispatcher message pumping.
Ideal for I/O-bound operations such as HTTP requests, file operations, or database queries during application startup and shutdown.

```c#
public class Application : AsyncExternalApplication
{
    public override async Task OnStartupAsync()
    {
        using var httpClient = new HttpClient();
        var configuration = await httpClient.GetStringAsync("https://api.example.com/config");

        var panel = Application.CreatePanel(configuration.PanelTitle);
        panel.AddPushButton<Command>(configuration.ButtonTitle);
    }

    public override async Task OnShutdownAsync()
    {
        await SaveSettingsAsync();
    }
}
```

#### ExternalDBApplication

Implementation for **IExternalDBApplication**. Same as `ExternalApplication` but without UI access:

```c#
public class Application : ExternalDBApplication
{
    public override void OnStartup()
    {
    }

    public override void OnShutdown()
    {
    }
}
```

### External events

The Toolkit provides `ExternalEvent` implementations for working with the Revit API from outside the execution context,
which is particularly useful when working with modeless windows.

Events do not need to be created inside the Revit API context before use — the Toolkit handles all the initialization for you,
so you can create them anywhere in your code and on any thread.

#### ExternalEvent

A synchronous external event that queues the handler via the Revit external event mechanism.
When `Raise()` is called, the delegate is placed into the Revit event queue and will be executed
in the next event-processing cycle, once Revit is ready and no other commands or edit modes are active.

```c#
private readonly ExternalEvent _deleteWindowsEvent = new(application =>
{
    var document = application.ActiveUIDocument.Document;
    using var transaction = new Transaction(document, "Delete windows");
    transaction.Start();
    document.Delete(document.GetInstanceIds(BuiltInCategory.OST_Windows));
    transaction.Commit();

    //2. The delegate body executes when Revit processes the event
});

private void DeleteWindows()
{
    _deleteWindowsEvent.Raise();

    //1. Raise returns immediately, the delegate is queued
}
```

#### ExternalEvent\<T>

A generic synchronous external event that accepts an argument of type `T`.
Works like `ExternalEvent`, but allows passing data to the handler at the time of raising.

```c#
private readonly ExternalEvent<ElementId> _deleteWindowEvent = new((application, elementId) =>
{
    var document = application.ActiveUIDocument.Document;
    using var transaction = new Transaction(document, "Delete window");
    transaction.Start();
    document.Delete(elementId);
    transaction.Commit();

    //2. The delegate body executes with the provided argument
});

private void DeleteWindow(ElementId elementId)
{
    _deleteWindowEvent.Raise(elementId);

    //1. Raise returns immediately, the delegate is queued
}
```

#### AsyncExternalEvent

An asynchronous external event that queues the handler and asynchronously awaits its completion.
The `RaiseAsync()` method returns a `Task` that completes when Revit has finished executing the delegate.

Exceptions thrown inside the delegate are rethrown in the original synchronization context.

> [!WARNING]
> Synchronously blocking the result of `RaiseAsync()` on the Revit main thread
> (e.g. `.Wait()`, `.Result`, `.GetAwaiter().GetResult()`) will cause a deadlock,
> because Revit cannot process the external event while its main thread is blocked by the waiting call.
> Use `await` instead, which releases the thread and allows Revit to process the event.

```c#
private readonly AsyncExternalEvent _deleteWindowsAsyncEvent = new(application =>
{
    var document = application.ActiveUIDocument.Document;
    using var transaction = new Transaction(document, "Delete windows");
    transaction.Start();
    document.Delete(document.GetInstanceIds(BuiltInCategory.OST_Windows));
    transaction.Commit();

    //1. The delegate body executes when Revit processes the event
});

private async Task DeleteWindowsAsync()
{
    await _deleteWindowsAsyncEvent.RaiseAsync();

    //2. Continues after the delegate has completed
}
```

#### AsyncExternalEvent\<T>

A generic asynchronous external event that accepts an argument of type `T` and asynchronously awaits completion.

```c#
private readonly AsyncExternalEvent<ElementId> _deleteWindowAsyncEvent = new((application, elementId) =>
{
    var document = application.ActiveUIDocument.Document;
    using var transaction = new Transaction(document, "Delete window");
    transaction.Start();
    document.Delete(elementId);
    transaction.Commit();

    //1. The delegate body executes with the provided argument
});

private async Task DeleteWindowAsync(ElementId elementId)
{
    await _deleteWindowAsyncEvent.RaiseAsync(elementId);

    //2. Continues after the delegate has completed
}
```

#### AsyncRequestExternalEvent\<TResult>

An asynchronous external event that returns a result of type `TResult` via `RaiseAsync()`.
The handler is queued to Revit and the returned `Task<TResult>` completes with the result once Revit has finished executing the delegate.

```c#
private readonly AsyncRequestExternalEvent<int> _countWindowsAsyncEvent = new(application =>
{
    var document = application.ActiveUIDocument.Document;
    var elementIds = document.GetInstanceIds(BuiltInCategory.OST_Windows);

    //1. The delegate body executes and returns a value
    return elementIds.Count;
});

private async Task CountWindowsAsync()
{
    var count = await _countWindowsAsyncEvent.RaiseAsync();

    //2. Continues after the delegate has completed, with the result
}
```

#### AsyncRequestExternalEvent\<T, TResult>

A generic asynchronous external event that accepts an argument of type `T`
and returns a result of type `TResult`.

```c#
private readonly AsyncRequestExternalEvent<ElementId, bool> _deleteWindowRequestEvent = new((application, elementId) =>
{
    var document = application.ActiveUIDocument.Document;
    using var transaction = new Transaction(document, "Delete window");
    transaction.Start();
    document.Delete(elementId);
    transaction.Commit();

    //1. The delegate body executes with the provided argument and returns a value
    return true;
});

private async Task DeleteWindowAsync(ElementId elementId)
{
    var result = await _deleteWindowRequestEvent.RaiseAsync(elementId);

    //2. Continues after the delegate has completed, with the result
}
```

#### ExternalEventOptions

You can configure the behavior of external events using `ExternalEventOptions`. 
The `AllowDirectInvocation` option enables the handler to be invoked directly on the calling thread when Revit is in API mode, instead of being queued:

```c#
private readonly ExternalEvent _deleteWindowsEvent = new(application =>
{
    //Execution logic
}, ExternalEventOptions.AllowDirectInvocation);
```

This option useful if you want support Modal and Modeless windows from a single codebase without wasting time on queue management and Revit event-processing cycle.

#### ExternalEvent attribute

The `ExternalEventAttribute` is an attribute that allows generating external event properties for annotated methods.
Its purpose is to completely eliminate the boilerplate that is needed to define external events wrapping private methods in a class.

**How it works**

The `ExternalEvent` attribute can be used to annotate a method in a partial type, like so:

```c#
public partial class MyViewModel
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
}
```

And it will generate properties like this:

```c#
public partial class MyViewModel
{
    public IExternalEvent DeleteWindowsEvent => field ??= new ExternalEvent(DeleteWindows);
    public IAsyncExternalEvent DeleteWindowsAsyncEvent => field ??= new AsyncExternalEvent(DeleteWindows);
}
```

After you can call a `Raise` method:

```c#
public partial class MyViewModel
{
    private void DeleteCommand()
    {
        DeleteWindowsEvent.Raise();
    }
}
```

> [!NOTE]
> The name of the generated properties is created based on the method name.
> The generator appends `Event` for the synchronous property and `AsyncEvent` for the asynchronous property.

**Methods without parameters**

For `void` methods, the generator creates both sync and async properties:

```c#
[ExternalEvent]
private void DeleteWindows() 
{
    _document.Delete(_windowIds);
}

// Generates:
// IExternalEvent DeleteWindowsEvent
// IAsyncExternalEvent DeleteWindowsAsyncEvent
```

For methods that return a value, only an async property is generated:

```c#
[ExternalEvent]
private int CountWindows() 
{
    return _document.GetInstanceIds(BuiltInCategory.OST_Windows).Count;
}

// Generates:
// IAsyncRequestExternalEvent<int> CountWindowsAsyncEvent
```

**Methods with UIApplication parameter**

When a method accepts a `UIApplication` parameter, the generated events pass the application instance to the handler:

```c#
[ExternalEvent]
private void DeleteWindows(UIApplication application)
{
    var document = application.ActiveUIDocument.Document;
    using var transaction = new Transaction(document, "Delete windows");
    transaction.Start();
    document.Delete(document.GetInstanceIds(BuiltInCategory.OST_Windows));
    transaction.Commit();
}

// Generates:
// IExternalEvent DeleteWindowsEvent
// IAsyncExternalEvent DeleteWindowsAsyncEvent
```

**Methods with one extra parameter**

When a method has one additional parameter beyond the optional `UIApplication`,
the generator creates typed event properties:

```c#
[ExternalEvent]
private void DeleteWindow(UIApplication application, ElementId elementId)
{
    var document = application.ActiveUIDocument.Document;
    using var transaction = new Transaction(document, "Delete window");
    transaction.Start();
    document.Delete(elementId);
    transaction.Commit();
}

// Generates:
// IExternalEvent<ElementId> DeleteWindowEvent
// IAsyncExternalEvent<ElementId> DeleteWindowAsyncEvent
```

For methods with a return value:

```c#
[ExternalEvent]
private int CountWindows(UIApplication application, BuiltInCategory category)
{
    return application.ActiveUIDocument.Document.GetInstanceIds(category).Count;
}

// Generates:
// IAsyncRequestExternalEvent<BuiltInCategory, int> CountWindowsAsyncEvent
```

**Methods with multiple extra parameters**

When a method has two or more extra parameters, the generator creates a `sealed record` to bundle them into a single argument type, along with extension methods to call Raise with individual parameters:

```c#
public partial class MyViewModel
{
    private Room CreateRoom(UIApplication application, Level level, UV coordinate)
    {
        var document = application.ActiveUIDocument.Document;
        using var transaction = new Transaction(document, "Create room");
        transaction.Start();
        var createdRoom = document.Create.NewRoom(level, coordinate);
        transaction.Commit();

        return createdRoom;
    }
}
```

Will generate:

```c#
public partial class MyViewModel
{
    public IAsyncRequestExternalEvent<CreateRoomArgs, Room> CreateRoomAsyncEvent => field ??= new IAsyncRequestExternalEvent<CreateRoomArgs, Room>(...);

    public sealed record CreateRoomArgs(Level Level, UV Coordinate);
}

public static partial class MyViewModelExtensions
{
    public static Task<Room> RaiseAsync(this IAsyncRequestExternalEvent<CreateRoomArgs, Room> externalEvent, Level level, UV coordinate)
    {
        return externalEvent.RaiseAsync(new CreateRoomArgs(level, coordinate));
    }
}
```

These extensions allow you to call the event with individual arguments instead of creating a new Args:

```c#
var room = await CreateRoomAsyncEvent.RaiseAsync(level, coordinate);
```

**Enabling direct invocation**

Use the `AllowDirectInvocation` property on the attribute to configure the generated events to execute directly when Revit is in API mode:

```c#
[ExternalEvent(AllowDirectInvocation = true)]
private void DeleteWindows(UIApplication application)
{
    //Execution logic
}
```

### Context

Interfaces to global information about an application environment.

It allows access to application-specific data, as well as up-calls for application-level operations such as dialog and failure handling.

#### RevitApiContext

Provides members for accessing the Revit application context at the database level.

List of available environment properties:

- RevitApiContext.Application

> [!NOTE]
> RevitApiContext data can be accessed from any application execution location.

**RevitApiContext** provides access to failure suppression using disposable scopes.

By default, Revit uses manual error resolution control with user interaction.
RevitApiContext provides automatic resolution of all failures without notifying the user or interrupting the program:

```C#
using (RevitApiContext.BeginFailureSuppressionScope())
{
    using var transaction = new Transaction(document, "Operation");
    transaction.Start();
    // Operations that may cause failures
    transaction.Commit();
}
// Failure handling is restored automatically
```

By default, all errors are handled for successful completion of the transaction.
However, if you want to cancel the transaction and undo all failed changes, pass false as the parameter:

```C#
using (RevitApiContext.BeginFailureSuppressionScope(resolveErrors: false))
{
    //User transactions
    ModifyDocument();
}
```

#### RevitContext

Provides members for accessing the Revit application context at the UI level.

List of available environment properties:

- RevitContext.UiApplication
- RevitContext.ActiveDocument
- RevitContext.ActiveUiDocument
- RevitContext.ActiveView
- RevitContext.ActiveGraphicalView
- RevitContext.IsRevitInApiMode

> [!NOTE]
> RevitContext data can be accessed from any application execution location.

If your application can run in a separate thread or use API requests in an asynchronous context, use **IsRevitInApiMode** property to verify Revit API context:

```C#
public void Execute()
{
    if (RevitContext.IsRevitInApiMode)
    {
        ModifyDocument();
    }
}
```

**RevitContext** provides access to dialog suppression using disposable scopes:

```C#
using (RevitContext.BeginDialogSuppressionScope())
{
    //User operations
    LoadFamilies();
}
// Dialogs are restored automatically
```

You can specify a result code for the suppressed dialogs:

```C#
using (RevitContext.BeginDialogSuppressionScope(resultCode: 2))
{
    LoadFamilies();
}

using (RevitContext.BeginDialogSuppressionScope(TaskDialogResult.Ok))
{
    LoadFamilies();
}

using (RevitContext.BeginDialogSuppressionScope(MessageBoxResult.Yes))
{
    LoadFamilies();
}
```

Or use a custom handler for more control:

```C#
using (RevitContext.BeginDialogSuppressionScope(args =>
{
    var result = args.DialogId == "TaskDialog_ModelUpdater" ? TaskDialogResult.Ok : TaskDialogResult.Close;
    args.OverrideResult((int)result);
}))
{
    LoadFamilies();
}
```

### Options

The Toolkit provides implementation of various Revit interfaces, with the possibility of customization.

#### FamilyLoadOptions

Contains an implementation for **IFamilyLoadOptions**.
Provides a handler for loading families

```c#
document.LoadFamily(fileName, new FamilyLoadOptions(), out var family);
document.LoadFamily(fileName, new FamilyLoadOptions(false, FamilySource.Project), out var family);
document.LoadFamily(fileName, UIDocument.GetRevitUIFamilyLoadOptions(), out var family);
```

#### DuplicateTypeNamesHandler

Contains an implementation for **IDuplicateTypeNamesHandler**.
Provides a handler of duplicate type names encountered during a paste operation.

```c#
var options = new CopyPasteOptions();
options.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler());
options.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler(args => DuplicateTypeAction.Abort));
options.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler(DuplicateTypeAction.UseDestinationTypes));
ElementTransformUtils.CopyElements(source, elementIds, destination, null, options);
```

#### SaveSharedCoordinatesCallback

Contains an implementation for **ISaveSharedCoordinatesCallback**.
Provides a handler for control Revit when trying to unload or reload a Revit link with changes in shared coordinates.

```c#
var linkType = elementId.ToElement<RevitLinkType>(RevitContext.ActiveDocument);
linkType.Unload(new SaveSharedCoordinatesCallback());
linkType.Unload(new SaveSharedCoordinatesCallback(SaveModifiedLinksOptions.DoNotSaveLinks));
linkType.Unload(new SaveSharedCoordinatesCallback(type =>
{
    if (type.AttachmentType == AttachmentType.Overlay) return SaveModifiedLinksOptions.SaveLinks;
    return SaveModifiedLinksOptions.DoNotSaveLinks;
}));
```

#### FrameworkElementCreator

Contains an implementation for **IFrameworkElementCreator**.
Creator of `FrameworkElements` for the dockable pane.

```c#
DockablePaneProvider.Register(application, guid, title)
    .SetConfiguration(data =>
    {
        data.FrameworkElementCreator = new FrameworkElementCreator<DockPaneView>();
        data.FrameworkElementCreator = new FrameworkElementCreator<DockPaneView>(serviceProvider);
    });
```

#### SelectionConfiguration

Contains an implementation for **ISelectionFilter**.
Creates a configuration for creating Selection Filters.

By default, all elements are allowed for selection:

```c#
var selectionConfiguration = new SelectionConfiguration();
uiDocument.Selection.PickObject(ObjectType.Element, selectionConfiguration.Filter);
```

You can also customize the selection of Element or Reference separately:

```c#
var selectionConfiguration = new SelectionConfiguration()
        .Allow.Element(element => element.Category.Id.AreEquals(BuiltInCategory.OST_Walls));

uiDocument.Selection.PickObject(ObjectType.Element, selectionConfiguration.Filter);
```

Or set rules for everything:

```c#
var selectionConfiguration = new SelectionConfiguration()
    .Allow.Element(element => element.Category.Id.AreEquals(BuiltInCategory.OST_Walls))
    .Allow.Reference((reference, xyz) => false);

uiDocument.Selection.PickObject(ObjectType.Element, selectionConfiguration.Filter);
```

### Decorators

Simplified implementation of raw Revit classes

#### DockablePaneProvider

Provides access to create a new dockable pane to the Revit user interface.

```c#
DockablePaneProvider
    .Register(application, new Guid(), "Dockable pane")
    .SetConfiguration(data =>
    {
        data.FrameworkElement = new RevitAddInView();
        data.InitialState = new DockablePaneState
        {
            MinimumWidth = 300,
            MinimumHeight = 400,
            DockPosition = DockPosition.Right
        };
    });
```

### Helpers

Provides auxiliary components

#### ResolveHelper

Provides handlers to resolve dependencies.

```c#
using (ResolveHelper.BeginAssemblyResolveScope<Application>())
{
    window.Show();
}
// Assembly resolution is restored automatically
```

You can also pass a type directly:

```c#
using (ResolveHelper.BeginAssemblyResolveScope(typeof(ViewModel)))
{
    return new Window();
}
```

Or specify a directory path directly:

```c#
using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Libraries"))
{
    return LoadExternalLibrary();
}
```

Scopes can be nested. Dependencies are searched from innermost to outermost scope:

```c#
using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Shared\Common"))
using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Plugin"))
{
    // First searches in Plugin, then in Common
    return new Window();
}
```

Enabled by default for `ExternalCommand`, `AsyncExternalCommand`, `ExternalApplication` and `ExternalDBApplication`.
