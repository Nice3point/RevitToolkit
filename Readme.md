<h3 align="center"><img src="https://user-images.githubusercontent.com/20504884/180418145-8a82f8ff-3649-4b80-a092-d67e9385e893.png" width="500px"></h3>

# That your Revit API could be more flexible now

<p align="center">
  <a href="https://www.nuget.org/packages/Nice3point.Revit.Toolkit"><img src="https://img.shields.io/nuget/v/Nice3point.Revit.Toolkit?style=for-the-badge"></a>
  <a href="https://www.nuget.org/packages/Nice3point.Revit.Toolkit"><img src="https://img.shields.io/nuget/dt/Nice3point.Revit.Toolkit?style=for-the-badge"></a>
  <a href="https://github.com/Nice3point/RevitToolkit/commits/develop"><img src="https://img.shields.io/github/last-commit/Nice3point/RevitToolkit/develop?style=for-the-badge"></a>
</p>

The lightweight library provides a modern interface for working with the Revit API. This package aims to offer as much flexibility as possible, so developers are free to choose
which components to use.

## Installation

You can install Extensions as a [nuget package](https://www.nuget.org/packages/Nice3point.Revit.Toolkit).

Packages are compiled for a specific version of Revit, to support different versions of libraries in one project, use RevitVersion property.

```text
<PackageReference Include="Nice3point.Revit.Toolkit" Version="$(RevitVersion).*"/>
```

Package included by default in [Revit Templates](https://github.com/Nice3point/RevitTemplates).

## Features

### Table of contents

- [External Handling](#ExternalHandling)
- [Transaction](#Transaction)
- [Options](#Options)

### <a id="ExternalHandling">External Handling</a>

The **ExternalEventHandler** class is used to modify the document when using modeless windows. It contains an implementation of the IExternalEventHandler interface. You can create your
own handlers by deriving from this class.

To avoid closures and increase performance, use generic overloads and pass data through parameters.

**ActionEventHandler**

With this handler, you can call your code from a lambda.

```c#
public ViewModel
{
    ActionEventHandler = new ActionEventHandler<ViewModel>();
    //Available overloads:
    //new ActionEventHandler()
    //new ActionEventHandler<T>()
    //new ActionEventHandler<T0, T1>()
    //new ActionEventHandler<T0, T1, T2>()
}

public ActionEventHandler<ViewModel> ActionEventHandler { get; set; }
public ElementId ElementId { get; set; }

private void DeteleElement()
{
    ActionEventHandler.Raise(this, (application, viewModel) =>
    {
        var document = application.ActiveUIDocument.Document;
        using var transaction = new Transaction(document, $"Delete element");
        transaction.Start();
        document.Delete(viewModel.ElementId)
        transaction.Commit();
    });
}
```

**IdlingEventHandler**

With this handler, you can call your code from the lambda when your application becomes available again. Unsubscribing from the Idling event occurs immediately. Suitable for cases
where you need to call code when Revit receives focus. For example, to display a window after loading a family into a project

```c#
public ViewModel
{
    IdlingEventHandler = new IdlingEventHandler();
    //Available overloads:
    //new IdlingEventHandler()
    //new IdlingEventHandler<T>()
    //new IdlingEventHandler<T0, T1>()
    //new IdlingEventHandler<T0, T1, T2>()
}

public IdlingEventHandler IdlingEventHandler { get; set; }

private void NotifyOnIdling()
{
    IdlingEventHandler.Raise(application =>
    {
        Log.Information("Application is available again");
    });
}
```

### <a id="Transaction">Transaction</a>

//TODO

### <a id="Options">Options</a>

//TODO

## Technology Sponsors

Thanks to [JetBrains](https://jetbrains.com) for providing licenses for [Rider](https://jetbrains.com/rider) and [dotUltimate](https://www.jetbrains.com/dotnet/) tools, which both
make open-source development a real pleasure!