# Release 2025.0.2-preview.2.0

- **Context** global handlers:
    - **SuppressFailures**: suppresses the display of the Revit error and warning messages during transaction.
      By default, Revit uses manual error resolution control with user interaction.
      This method provides automatic resolution of all failures without notifying the user or interrupting the program
    - **SuppressDialogs**: suppresses the display of the Revit dialogs
- **Context** global properties:
    - **IsRevitInApiMode**: determines whether Revit is in API mode or not.
- **ExternalCommand.SuppressFailures** is obsolete. Use Context class instead.
- **ExternalCommand.SuppressDialogs** is obsolete. Use Context class instead.
- **ActionEventHandler** now understands when it is in a Revit context and calls the handler immediately, without adding to the queue.
- **AsyncEventHandler** now understands when it is in a Revit context and calls the handler immediately, without adding to the queue.
  Awaiting with `await` keyword will not cause a context switch, and you can still call API requests in the main Revit thread.
- Nullable types support
- Assembly load context internal optimizations
- Readme updates, include extra samples

# Release 2025.0.1

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

# Release 2025.0.0

- Revit 2025 support
- Add new DuplicateTypeNamesHandler overload
- Optimize assembly resolve by @dosymep
- Fix resolve raw assembly by @dosymep

# Release 2024.0.1

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

# Release 2024.0.0

- Revit 2024 support
- New SaveSharedCoordinatesCallback
- New ExternalDbApplication

ExternalCommand:

- New SuppressFailures method

# Release 2023.0.12

New runtime attributes: init, required keywords support

# Release 2023.0.11

- New AsyncEventHandler
- New AsyncEventHandler<T>
- New ResolveHelper.ResolveAssembly
- New FamilyLoadOptions overloads

- Updated ExternalCommand
- Updated ExternalApplication

- Fixed ExternalEventHandler performance

- Removed ActionEventHandler params versions. Use parameterless version
- Removed IdlingEventHandler params versions. Use parameterless version

# Release 2023.0.10

Dependency resolver for ExternalApplication

Nuget symbol server support: https://symbols.nuget.org/download/symbols

# Release 2023.0.9

TransactionUtils:

- Updated transaction mechanism
- New Modify document method

# Release 2023.0.8

Fixed security issues

# Release 2023.0.7

Fixed dependency search issue for several plugins loaded in Revit

# Release 2023.0.6

- External command minor backend optimization

# Release 2023.0.5

External command:

- New SuppressExceptions() method
- New SuppressDialogs() method
- New ActiveView property
- New Application property
- Updated Assembly resolver
- Removed ExceptionHandler

# Release 2023.0.4

- Change namespace for DockablePaneProvider

# Release 2023.0.3

- New ExternalApplication

# Release 2023.0.2

- New ExternalCommand

# Release 2023.0.1

- New DockablePaneProvider
- New SelectionConfiguration

# Release 2023.0.0

- Initial release