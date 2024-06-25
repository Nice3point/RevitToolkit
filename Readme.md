<p align="center">
    <picture>
        <source media="(prefers-color-scheme: dark)" width="610" srcset="https://github.com/Nice3point/RevitToolkit/assets/20504884/852aba24-118f-4908-949d-2e0c019c83da">
        <img alt="RevitLookup" width="610" src="https://github.com/Nice3point/RevitToolkit/assets/20504884/c59042df-b9b5-4829-9417-006912781cf2">
    </picture>
</p>

## Make Revit API more flexible

[![Nuget](https://img.shields.io/nuget/v/Nice3point.Revit.Toolkit?style=for-the-badge)](https://www.nuget.org/packages/Nice3point.Revit.Toolkit)
[![Downloads](https://img.shields.io/nuget/dt/Nice3point.Revit.Toolkit?style=for-the-badge)](https://www.nuget.org/packages/Nice3point.Revit.Toolkit)
[![Last Commit](https://img.shields.io/github/last-commit/Nice3point/RevitToolkit/develop?style=for-the-badge)](https://github.com/Nice3point/RevitToolkit/commits/develop)

This library provides a modern interface for working with the Revit API. 
Package contains interfaces implementation frequently encountered in revit, aiming to provide as much flexibility as possible, so developers are free to choose which components to use.

## Installation

You can install Toolkit as a [nuget package](https://www.nuget.org/packages/Nice3point.Revit.Toolkit).

Packages are compiled for a specific version of Revit, to support different versions of libraries in one project, use RevitVersion property.

```text
<PackageReference Include="Nice3point.Revit.Toolkit" Version="$(RevitVersion).*"/>
```

Package included by default in [Revit Templates](https://github.com/Nice3point/RevitTemplates).

## Table of contents

* [External command](#externalcommand)
* [External application](#externalapplication)
* [External DB application](#externaldbapplication)
* [External events](#external-events)
    * [ActionEventHandler](#actioneventhandler)
    * [IdlingEventHandler](#idlingeventhandler)
    * [AsyncEventHandler](#asynceventhandler)
    * [AsyncEventHandler\<T>](#asynceventhandlert)
* [External Command Availability](#external-command-availability)
    * [AvailableCommandController](#availablecommandcontroller)
* [Context](#context)
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
    * [Add-ins Dependency Isolation](#add-ins-dependency-isolation)

## Features

### ExternalCommand

Contains an implementation for **ExternalCommand**.

```c#
[Transaction(TransactionMode.Manual)]
public class Command : ExternalCommand
{
    public override void Execute()
    {
    }
}
```

Override method **Execute()** to implement and external command within Revit.

You can suppress the display of exceptions, dialog boxes, without subscribing to events.

Data available when executing an external command is accessible by properties.

```c#
[Transaction(TransactionMode.Manual)]
public class Command : ExternalCommand
{
    public override void Execute()
    {
        var title = Document.Title;
        var viewName = ActiveView.Name;
        var username = Application.Username;
        var selection = UiDocument.Selection;
        var windowHandle = UiApplication.MainWindowHandle;

        if (title.Equals("Untitled"))
        {
            Result = Result.Cancelled;
            return;
        }

        SuppressDialogs();
        SuppressDialogs(args => args.OverrideResult(2));
        RestoreDialogs();

        SuppressFailures();
        RestoreFailures();
    }
}
```

**ExternalCommand** contains the logic for resolving dependencies. Now you may not encounter a FileNotFoundException. Dependencies are searched in the plugin folder.

Starting with Revit 2025, ExternalCommand uses **AssemblyLoadContext** to isolate dependencies.
This feature allows plugins to run in a separate, isolated context, ensuring independent operation and preventing conflicts from incompatible library versions

### ExternalApplication

Contains an implementation for **IExternalApplication**.

```c#
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
    }

    public override void OnShutdown()
    {
    }
}
```

Override method **OnStartup()** to execute some tasks when Revit starts.

Override method **OnShutdown()** to execute some tasks when Revit shuts down. You don't have to override this method if you don't plan to use it.

Data available when executing an external command is accessible by properties.

```c#
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        var userName = Context.Application.Username;
        if (userName != "Nice3point")
        {
            //If Result is overridden, the OnShutdown() method will not be called
            Result = Result.Failed;
            return;
        }
        
        var panel = Application.CreatePanel("Secret Panel", "RevitAddin");
        var showButton = panel.AddPushButton<Command>("Execute");
        showButton.SetImage("/RevitAddin;component/Resources/Icons/RibbonIcon16.png");
        showButton.SetLargeImage("/RevitAddin;component/Resources/Icons/RibbonIcon32.png");
    }
}
```

**ExternalApplication** contains the logic for resolving dependencies. Now you may not encounter a FileNotFoundException. Dependencies are searched in the plugin folder.

Starting with Revit 2025, ExternalApplication uses **AssemblyLoadContext** to isolate dependencies.
This feature allows plugins to run in a separate, isolated context, ensuring independent operation and preventing conflicts from incompatible library versions

### ExternalDBApplication

Contains an implementation for **IExternalDBApplication**.

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

Override method **OnStartup()** to execute some tasks when Revit starts.

Override method **OnShutdown()** to execute some tasks when Revit shuts down. You don't have to override this method if you don't plan to use it.

**ExternalDBApplication** contains the logic for resolving dependencies. Now you may not encounter a FileNotFoundException. Dependencies are searched in the plugin folder.

Starting with Revit 2025, ExternalDBApplication uses **AssemblyLoadContext** to isolate dependencies.
This feature allows plugins to run in a separate, isolated context, ensuring independent operation and preventing conflicts from incompatible library versions

### External events

Contains an implementations for **IExternalEventHandler**.

It is used to modify the document when using modeless windows. 
You can create your own handlers by deriving from this class.

#### ActionEventHandler

With this handler, you can queue delegates for method calls:

```c#
public ViewModel
{
    ActionEventHandler = new ActionEventHandler();
}

public ActionEventHandler ActionEventHandler { get; }
public ElementId ElementId { get; set; }

private void DeteleElement()
{
    ActionEventHandler.Raise(application =>
    {
        var document = application.ActiveUIDocument.Document;
        using var transaction = new Transaction(document, $"Delete element");
        transaction.Start();
        document.Delete(ElementId)
        transaction.Commit();
        
        Debug.WriteLine("Deleted");
    });

    Debug.WriteLine("Command completed");
}
```

Debug output:

```text
Command completed
Deleted
```

#### IdlingEventHandler

With this handler, you can queue delegates for method calls when Revit becomes available again.
Unsubscribing from the Idling event occurs immediately. 
Suitable for cases where you need to call code when Revit receives focus.
For example, to display a window after loading a family into a project.

```c#
public ViewModel
{
    IdlingEventHandler = new IdlingEventHandler();
}

public IdlingEventHandler IdlingEventHandler { get; }

private void NotifyOnIdling()
{
    IdlingEventHandler.Raise(application =>
    {
        var view = new FamilyBrowser();
        view.Show();
        
        Debug.WriteLine("Idling");
    });

    Debug.WriteLine("Command completed");
}
```

Debug output:

```text
Command completed
Idling
```

#### AsyncEventHandler

With this handler, you can wait for the external event to complete.
The **RaiseAsync** method will return to its previous context after executing the method encapsulated in the delegate.
Suitable for cases where you need to maintain the sequence of code execution.

Exceptions in the delegate will not be ignored and will be rethrown in the original synchronization context.

```c#
public ViewModel
{
    AsyncEventHandler = new AsyncEventHandler();
}

public AsyncEventHandler AsyncEventHandler { get; }

private async Task DeleteDoorsAsync()
{
    await AsyncEventHandler.RaiseAsync(application =>
    {
        var doorIds = document.GetInstanceIds(BuiltInCategory.OST_Doors);
        document.Delete(doorIds);

        Debug.WriteLine("Doors deleted");
    });

    Debug.WriteLine("Command completed");
}
```

Debug output:

```text
Doors deleted
Command completed
```

#### AsyncEventHandler\<T>

With this handler, you can wait for the external event to complete with the return value from the method encapsulated in the delegate.
The **RaiseAsync** method will return to its previous context after executing.
Suitable for cases where you need to maintain the sequence of code execution.

Exceptions in the delegate will not be ignored and will be rethrown in the original synchronization context

```c#
public ViewModel
{
    AsyncEventHandler = new AsyncEventHandler<int>();
}

public AsyncEventHandler<int> AsyncEventHandler { get; }

private async Task GetWindowsCountAsync()
{
    var windowsCount = await AsyncEventHandler.RaiseAsync(application =>
    {
        var uiDocument = application.ActiveUIDocument;
        var elementIds = uiDocument.Document.GetInstanceIds(BuiltInCategory.OST_Windows);
        uiDocument.Selection.SetElementIds(elementIds);

        Debug.WriteLine("Windows selected");
        return elementIds.Count;
    });

    Debug.WriteLine($"Windows count {windowsCount}");
    Debug.WriteLine("Command completed");
}
```

Debug output:

```text
Windows selected
Windows count 17
Command completed
```

### External Command Availability

Contains an implementation for **IExternalCommandAvailability**.

It provides the accessibility check for a Revit add-in External Command.

Starting with Revit 2025, ExternalCommandAvailability uses **AssemblyLoadContext** to isolate dependencies.
If your implementation does not include dependencies, use the **IExternalCommandAvailability** interface to reduce memory allocation

#### AvailableCommandController

Controller providing permanent accessibility for External Command invocation. This means that it will always be available for execution, even when no Document is open

```C#
panel.AddPushButton<StartupCommand>("Execute")
    .SetAvailabilityController<AvailableCommandController>()
```

### Context

Provides computed properties to retrieve Revit objects in the current session. 
Values are provided even outside the Revit context.

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
var linkType = elementId.ToElement<RevitLinkType>(Context.Document);
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

Provides handlers to resolve dependencies for Revit 2024 and older.

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

Enabled by default for `ExternalCommand`, `ExternalApplication` and `ExternalDBApplication`.

#### Add-ins Dependency Isolation

Provides dependency isolation for Revit 2025 and earlier.

This library enables running plugins in an isolated context using .NET [AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext). 
Each plugin executes independently, preventing conflicts from incompatible library versions. 

How It Works:

The core functionality centers on `AssemblyLoadContext`, which creates an isolated container for each plugin.
When a plugin is loaded, it is assigned a unique `AssemblyLoadContext` instance, encapsulating the plugin and its dependencies to prevent interference with other plugins or the main application.

To use this isolation feature, developers must inherit their classes from:
- ExternalCommand
- ExternalApplication
- ExternalDbApplication
- ExternalCommandAvailability

These classes contain the built-in isolation mechanism under the hood.
Plugins using interfaces such as `IExternalCommand` will not benefit from this isolation and will run in the default context.

Limitations:

- The isolated context feature is available starting with Revit 2025.
- For older Revit versions, this library uses a `ResolveHelper` to help load dependencies from the plugin's folder, but does not protect against conflicts arising from incompatible packages.
- Additionally, plugins that do not inherit from the specified classes will not be isolated and may experience compatibility issues if they rely on the default context.