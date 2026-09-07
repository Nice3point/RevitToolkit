<p align="center">
    <picture>
        <source media="(prefers-color-scheme: dark)" width="610" srcset="https://github.com/Nice3point/RevitToolkit/assets/20504884/852aba24-118f-4908-949d-2e0c019c83da">
        <img alt="Revit Toolkit" width="610" src="https://github.com/Nice3point/RevitToolkit/assets/20504884/c59042df-b9b5-4829-9417-006912781cf2">
    </picture>
</p>

## Make Revit API more flexible

[![Nuget](https://img.shields.io/nuget/vpre/Nice3point.Revit.Toolkit?style=for-the-badge&color=1A1A1A&labelColor=C42A2A)](https://www.nuget.org/packages/Nice3point.Revit.Toolkit)
[![Downloads](https://img.shields.io/nuget/dt/Nice3point.Revit.Toolkit?style=for-the-badge&color=1A1A1A&labelColor=C42A2A)](https://www.nuget.org/packages/Nice3point.Revit.Toolkit)
[![Last Commit](https://img.shields.io/github/last-commit/Nice3point/RevitToolkit/develop?style=for-the-badge&color=1A1A1A&labelColor=C42A2A)](https://github.com/Nice3point/RevitToolkit/commits/develop)

This library provides a modern interface for working with the Revit API.
Package contains interfaces implementation frequently encountered in revit, and developers are free to choose which components to use.

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

An implementation for **IExternalCommand**.
Use `ExternalCommand` for synchronous operations.
Access the current `UIApplication` through the inherited `Application` property:

```c#
[Transaction(TransactionMode.Manual)]
public class Command : ExternalCommand
{
    public override void Execute()
    {
        var document = Application.ActiveUIDocument.Document;
        TaskDialog.Show("Active document", document.Title);
    }
}
```

#### AsyncExternalCommand

An implementation for asynchronous **IExternalCommand**.
Use `AsyncExternalCommand` when a command awaits HTTP requests, file access, or other asynchronous work.
Override `ExecuteAsync()` and use `await` for those operations.
The base class keeps the Revit UI responsive while awaiting completion.

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

An implementation for **IExternalApplication**.
Override `OnStartup()` to create ribbon controls or register event handlers.
Override `OnShutdown()` when the add-in needs to release resources or save the state.

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
}
```

#### AsyncExternalApplication

An implementation for asynchronous **IExternalApplication**.
Use `AsyncExternalApplication` when startup or shutdown needs asynchronous work.
Override `OnStartupAsync()` and, when needed, `OnShutdownAsync()`.
The Revit UI remains responsive while these methods await completion.

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

An implementation for **IExternalDBApplication**.
Use `ExternalDBApplication` for a database-level add-in that doesn't create UI controls.
Its `Application` property exposes `ControlledApplication`.

```c#
public class DocumentLoggingApplication : ExternalDBApplication
{
    public override void OnStartup()
    {
        Application.DocumentOpened += OnDocumentOpened;
    }

    public override void OnShutdown()
    {
        SaveSettings();
    }
}
```

### External events

The Toolkit provides `ExternalEvent` implementations for working with the Revit API from outside the execution context,
which is particularly useful when working with modeless windows.

Events do not need to be created inside the Revit API context before use — the Toolkit handles all the initialization for you,
so you can create them anywhere in your code and on any thread.

Use `Raise()` when the caller does not need to wait, or `RaiseAsync()` when it needs completion or a return value.
For generated event properties, see the [ExternalEvent attribute](#externalevent-attribute).

#### ExternalEvent

A synchronous external event that queues the handler via the Revit external event mechanism.
When `Raise()` is called, the delegate is placed into the Revit event queue and will be executed in the next event-processing cycle,
once Revit is ready and no other commands or edit modes are active.

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

private async Task<int> CountWindowsAsync()
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

private async Task<ElementId, bool> DeleteWindowAsync(ElementId elementId)
{
    var result = await _deleteWindowRequestEvent.RaiseAsync(elementId);

    //2. Continues after the delegate has completed, with the result
}
```

> [!IMPORTANT]
> Await asynchronous events. Blocking a pending event with `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()` on the Revit main thread prevents Revit from processing it and can deadlock.
> The event handler itself remains synchronous; perform asynchronous I/O before or after raising the event.

#### ExternalEventOptions

You can configure the behavior of external events using `ExternalEventOptions`.
The `AllowDirectInvocation` option enables the handler to be invoked directly on the calling thread when Revit is in API mode, instead of being queued:

```c#
private readonly ExternalEvent _showDocumentTitleEvent = new(application =>
{
    TaskDialog.Show("Active document", application.ActiveUIDocument.Document.Title);
}, ExternalEventOptions.AllowDirectInvocation);
```

Use this option when the same operation is called from both a Revit callback and a modeless window.

#### ExternalEvent attribute

The `ExternalEventAttribute` generates external event properties for annotated methods.
Add `[ExternalEvent]` to your handler, then call the generated property's `Raise()` or `RaiseAsync()` method to run it in the Revit API context.

**How it works**

Declare the containing class as `partial` and annotate the method:

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

The generator adds `DeleteWindowsEvent` and `DeleteWindowsAsyncEvent` properties.
Call either property from your code:

```c#
DeleteWindowsEvent.Raise();

// Wait for the handler to finish:
await DeleteWindowsAsyncEvent.RaiseAsync();
```

> [!NOTE]
> Property names follow the handler name: `Event` for `Raise()` and `AsyncEvent` for `RaiseAsync()`.
> Changes to the document still require a transaction inside the handler.

**Methods without parameters**

For `void` methods, the generator creates both sync and async properties:

```c#
[ExternalEvent]
private void ShowMessage()
{
    TaskDialog.Show("Toolkit", "The handler runs in Revit.");
}

// Generates:
// IExternalEvent ShowMessageEvent
// IAsyncExternalEvent ShowMessageAsyncEvent
```

For a method that returns a value, the generator creates an async property.
Await `RaiseAsync()` to receive the result:

```c#
[ExternalEvent]
private int CountWindows()
{
    return _document.GetInstanceIds(BuiltInCategory.OST_Windows).Count;
}

// Generates:
// IAsyncRequestExternalEvent<int> CountWindowsAsyncEvent
```

```c#
var count = await CountWindowsAsyncEvent.RaiseAsync();
```

**Methods with UIApplication parameter**

Place `UIApplication` first when the handler needs access to the application.
The event supplies it automatically; omit it when calling `Raise()` or `RaiseAsync()`:

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

For one parameter beyond the optional `UIApplication`, pass its value directly to the generated event:

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

```c#
await DeleteWindowAsyncEvent.RaiseAsync(elementId);
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

For multiple parameters, pass the arguments in the same order as the handler declaration.
The optional first `UIApplication` parameter is still supplied by the event:

```c#
public partial class MyViewModel
{
    [ExternalEvent]
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

```c#
var room = await CreateRoomAsyncEvent.RaiseAsync(level, coordinate);
```

The generator provides a `CreateRoomArgs` record and extension methods that accept the individual arguments.
Import the view model's namespace when calling these extensions from another namespace.

<details>
<summary>Generated members for this example</summary>

This example uses C# 14 and .NET 9 or later.
Each event is initialized on first access; concurrent callers receive the same instance.

```c#
public partial class MyViewModel
{
    private global::System.Threading.Lock? _CreateRoomAsyncEventLock;

    public IAsyncRequestExternalEvent<CreateRoomArgs, Room> CreateRoomAsyncEvent
    {
        get
        {
            lock (global::System.Threading.LazyInitializer.EnsureInitialized(ref _CreateRoomAsyncEventLock))
            {
                return field ??= new AsyncRequestExternalEvent<CreateRoomArgs, Room>((application, args) =>
                {
                    return CreateRoom(application, args.Level, args.Coordinate);
                });
            }
        }
    }

    public sealed record CreateRoomArgs(Level Level, UV Coordinate);
}

public static partial class MyViewModelExtensions
{
    public static global::System.Threading.Tasks.Task<Room> RaiseAsync(this IAsyncRequestExternalEvent<MyViewModel.CreateRoomArgs, Room> externalEvent, Level level, UV coordinate)
    {
        return externalEvent.RaiseAsync(new MyViewModel.CreateRoomArgs(level, coordinate));
    }
}
```

Older language versions use an explicit backing field, and targets without C# 13 or `System.Threading.Lock` use an object lock.
If initialization throws, the next access retries it.

</details>

**Enabling direct invocation**

Set `AllowDirectInvocation` to execute the handler directly when Revit is already in API mode.
Calls made outside that context are queued as external events:

```c#
[ExternalEvent(AllowDirectInvocation = true)]
private void DeleteWindows(UIApplication application)
{
    var document = application.ActiveUIDocument.Document;
    using var transaction = new Transaction(document, "Delete windows");
    transaction.Start();
    document.Delete(document.GetInstanceIds(BuiltInCategory.OST_Windows));
    transaction.Commit();
}
```

**Generic and nested types**

The containing type can be generic.
Its type parameters and constraints remain available to the generated events:

```c#
public partial class ElementNameReader<TElement> where TElement : Element
{
    [ExternalEvent]
    private string GetName(TElement element)
    {
        return element.Name;
    }
}
```

```c#
var nameReader = new ElementNameReader<Wall>();
var name = await nameReader.GetNameAsyncEvent.RaiseAsync(wall);
```

For nested types, mark every containing type as `partial`.
Private and protected nested types use the generated argument record directly when a handler has multiple parameters: `RunAsyncEvent.RaiseAsync(new RunArgs(first, second))`.

**Handler requirements**

- The handler must be synchronous and non-generic. Use `RaiseAsync()` to await its execution; the handler itself must not return `Task` or use `async void`.
- Parameters must be passed by value. `ref`, `in`, `out`, and types such as `Span<T>` cannot be stored in an external event.
- Give each annotated handler a unique name within its type and leave its generated member names available.
- For instance handlers on structs, the event captures a copy of the struct when first initialized. Later changes to the original struct are not reflected in that event.

The IDE reports unsupported declarations and offers fixes where the correction is unambiguous.

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
    using var transaction = new Transaction(document, "Delete elements");
    transaction.Start();
    document.Delete(elementIds);
    transaction.Commit();
}
```

#### RevitContext

Provides members for accessing the Revit application context at the UI level.

`RevitContext` exposes the current `UI application`, document, and view.
Use `UiApplication` for UI operations, `ActiveUiDocument` for selection, and `ActiveDocument` for database access.
`ActiveView` and `ActiveGraphicalView` expose the corresponding views.
The active document and view properties can be `null` when no document is open:

```C#
var document = RevitContext.ActiveDocument;
if (document is null)
{
    return;
}

TaskDialog.Show("Active document", document.Title);
```

These accessors do not make Revit API calls safe on a background thread.
`IsRevitInApiMode` reports API mode; it does not enter that mode or dispatch work to Revit.

```C#
if (RevitContext.IsRevitInApiMode)
{
    ModifyDocument();
}
```

Use an [external event](#external-events) when calling from a modeless window or background code.

**Suppressing dialogs**

Wrap an operation in `BeginDialogSuppressionScope()` to override dialog results temporarily.
This example supplies the default result code while loading a family:

```C#
using (RevitContext.BeginDialogSuppressionScope())
{
    document.LoadFamily(fileName, out var family);
}
```

Suppression ends when the last active scope is disposed.
Pass an explicit result when the dialog requires a different response:

```C#
using (RevitContext.BeginDialogSuppressionScope(TaskDialogResult.Ok))
{
    document.LoadFamily(fileName, out var family);
}
```

The overloads accept `TaskDialogResult`, `MessageBoxResult`, or a numeric result code.
The result must match a button supported by the dialog.
For operations that can display different dialogs, use a callback and override only the ones you recognize:

```C#
using (RevitContext.BeginDialogSuppressionScope(args =>
{
    if (args.DialogId == "TaskDialog_ModelUpdater")
    {
        args.OverrideResult((int)TaskDialogResult.Ok);
    }
}))
{
    document.LoadFamily(fileName, out var family);
}
```

### Options

The Toolkit provides implementation of various Revit interfaces.
Each provides default behavior and constructor arguments or delegates for customization.

#### FamilyLoadOptions

An implementation for **IFamilyLoadOptions**.
Pass `FamilyLoadOptions` to `Document.LoadFamily` to control how an existing family is updated.
The default options continue loading, overwrite parameter values of existing types, and use the incoming family for shared-family conflicts:

```c#
document.LoadFamily(fileName, new FamilyLoadOptions(), out var family);
```

To preserve existing parameter values and use the project's shared family:

```c#
var loadOptions = new FamilyLoadOptions(overwrite: false, familySource: FamilySource.Project);
document.LoadFamily(fileName, loadOptions, out var family);
```

Call family-loading operations from the Revit API context with the transaction requirements of the chosen `LoadFamily` overload.

#### DuplicateTypeNamesHandler

An implementation for **IDuplicateTypeNamesHandler**.
Attach `DuplicateTypeNamesHandler` to copy options when copying elements between documents.
By default, name conflicts use the destination document's types:

```c#
using var options = new CopyPasteOptions();
options.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler());
ElementTransformUtils.CopyElements(source, elementIds, destination, null, options);
```

To cancel the copy when duplicate type names are encountered, configure the options before copying:

```c#
options.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler(DuplicateTypeAction.Abort));
```

For a decision based on the conflicting types, pass a `Func<DuplicateTypeNamesHandlerArgs, DuplicateTypeAction>` callback.

#### SaveSharedCoordinatesCallback

An implementation for **ISaveSharedCoordinatesCallback**.
Use `SaveSharedCoordinatesCallback` when unloading or reloading a Revit link with modified shared coordinates.
The default callback saves the changes:

```c#
linkType.Unload(new SaveSharedCoordinatesCallback());
```

Pass a fixed option to skip saving:

```c#
linkType.Unload(new SaveSharedCoordinatesCallback(SaveModifiedLinksOptions.DoNotSaveLinks));
```

Or choose an option for each link:

```c#
linkType.Unload(new SaveSharedCoordinatesCallback(type =>
{
    return type.AttachmentType == AttachmentType.Overlay
        ? SaveModifiedLinksOptions.SaveLinks
        : SaveModifiedLinksOptions.DoNotSaveLinks;
}));
```

#### FrameworkElementCreator

An implementation for **IFrameworkElementCreator**.
Use `FrameworkElementCreator<T>` when Revit should create a dockable pane's WPF content on demand.
Without a service provider, `T` must derive from `FrameworkElement` and have a public parameterless constructor:

```c#
DockablePaneProvider.Register(application, guid, title)
    .SetConfiguration(data =>
    {
        data.FrameworkElementCreator = new FrameworkElementCreator<DockPaneView>();
    });
```

To resolve the view through dependency injection, pass an `IServiceProvider` instead:

```c#
data.FrameworkElementCreator = new FrameworkElementCreator<DockPaneView>(serviceProvider);
```

Register `DockPaneView` with the provider before Revit requests it.

#### SelectionConfiguration

An implementation for **ISelectionFilter**.
Use `SelectionConfiguration` to define selection rules with delegates.
Pass its `Filter` property to Revit's selection methods.
With no rules configured, the filter accepts every candidate:

```c#
var selectionConfiguration = new SelectionConfiguration();
uiDocument.Selection.PickObject(ObjectType.Element, selectionConfiguration.Filter);
```

To restrict selection to walls:

```c#
var selectionConfiguration = new SelectionConfiguration()
    .Allow.Element(element => element is Wall);

uiDocument.Selection.PickObject(ObjectType.Element, selectionConfiguration.Filter);
```

Use `Allow.Reference` to add a separate rule for geometry references.
Its callback receives the candidate `Reference` and cursor position as `XYZ`.

```c#
var selectionConfiguration = new SelectionConfiguration()
    .Allow.Element(element => element is Wall)
    .Allow.Reference((reference, xyz) => true);

uiDocument.Selection.PickObject(ObjectType.Element, selectionConfiguration.Filter);
```

### Decorators

Use the decorators to configure Revit components without implementing their provider interfaces yourself.

#### DockablePaneProvider

Register a dockable pane during your external application's `OnStartup()`.
Supply a stable GUID, a title, the WPF content, and its initial docking position:

```c#
var paneGuid = new Guid("6b8bd014-f488-4555-925d-e56be2c25b76");

DockablePaneProvider
    .Register(application, paneGuid, "Project tools")
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

Use a GUID unique to your pane and keep it unchanged between runs.
To display the registered pane from a command:

```c#
var pane = Application.GetDockablePane(new DockablePaneId(paneGuid));
pane.Show();
```

### Helpers

Helpers support loading add-in dependencies.

#### ResolveHelper

Use `ResolveHelper.BeginAssemblyResolveScope<T>()` around code that loads dependencies from the directory containing `T`'s assembly:

```c#
using (ResolveHelper.BeginAssemblyResolveScope<AddInApplication>())
{
    window.ShowDialog();
}
```

The resolution handler remains active for the lifetime of the scope.
For a modal window, keep the scope open until `ShowDialog()` returns; a scope around `Show()` ends as soon as the modeless window opens.

Pass a `Type` when the assembly is selected at runtime:

```c#
using (ResolveHelper.BeginAssemblyResolveScope(typeof(ViewModel)))
{
    return new Window();
}
```

Or specify the directory to search:

```c#
using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Libraries"))
{
    return LoadExternalLibrary();
}
```

Scopes can be nested. Dependencies are searched from the innermost scope outward:

```c#
using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Shared\Common"))
using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Plugin"))
{
    // First searches in Plugin, then in Common
    return new Window();
}
```

The command and application base classes already manage dependency resolution during their callbacks when needed.
Add a scope for work outside those callbacks that requires the same directory-based resolution.
