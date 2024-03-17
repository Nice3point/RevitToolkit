# Release 2025.0.0-preview.2.0

- New package icon
- Add new DuplicateTypeNamesHandler overload

# Release 2025.0.0-preview.1.1

- Fix assembly resolver for Revit 2025

# Release 2025.0.0-preview.1.0

- Revit 2025 support
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