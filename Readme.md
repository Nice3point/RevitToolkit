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

The **ExternalEventHandler** class is used to modify the document when using modeless windows. It contains an implementation of the IExternalEventHandler interface. You can create
your
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

The TransactionManager gives you the ability to create transactions. You can write custom code in a lambda, and the method will take care of safely closing transactions in case of
exceptions and cleaning up unmanaged resources.

To avoid closures and increase performance, use generic overloads and pass data through parameters.

**CreateSubTransaction**

```c#
TransactionManager.CreateSubTransaction(application.ActiveUIDocument.Document, document =>
{
    document.Delete(new ElementId(106234));
});

//Available overloads:
//CreateSubTransaction(Document document, Action<Document> action);
//CreateSubTransaction<T>(Document document, T param, Action<Document, T> action)
//CreateSubTransaction<T0, T1>(Document document, T0 param0, T1 param1, Action<Document, T0, T1> action)
//CreateSubTransaction<T0, T1, T2>(Document document, T0 param0, T1 param1, T2 param2, Action<Document, T0, T1, T2> action)
```

**CreateTransaction**

```c#
public ElementId ElementId { get; set; }

private void DeteleElement()
{
    TransactionManager.CreateTransaction(application.ActiveUIDocument.Document, "Delete element", this, (document, viewModel) =>
    {
        document.Delete(viewModel.ElementId);
    });
}

//Available overloads:
//CreateTransaction(Document document, string transactionName, Action<Document> action);
//CreateTransaction<T>(Document document, string transactionName, T param, Action<Document, T> action)
//CreateTransaction<T0, T1>(Document document, string transactionName, T0 param0, T1 param1, Action<Document, T0, T1> action)
//CreateTransaction<T0, T1, T2>(Document document, string transactionName, T0 param0, T1 param1, T2 param2, Action<Document, T0, T1, T2> action)
```

**CreateGroupTransaction**

```c#
public ElementId ElementId1 { get; set; }
public ElementId ElementId2 { get; set; }

private void DeteleElements()
{
    TransactionManager.CreateGroupTransaction(application.ActiveUIDocument.Document, "Delete elements", this, (document, viewModel) =>
    {
        TransactionManager.CreateTransaction(application.ActiveUIDocument.Document, "Delete element", viewModel, (document, vm) =>
        {
            document.Delete(vm.ElementId1);
        });
        
        TransactionManager.CreateTransaction(application.ActiveUIDocument.Document, "Delete element", viewModel, (document, vm) =>
        {
            document.Delete(vm.ElementId2);
        });
    });
}

//Available overloads:
//CreateGroupTransaction(Document document, string transactionName, Action<Document> action);
//CreateGroupTransaction<T>(Document document, string transactionName, T param, Action<Document, T> action)
//CreateGroupTransaction<T0, T1>(Document document, string transactionName, T0 param0, T1 param1, Action<Document, T0, T1> action)
//CreateGroupTransaction<T0, T1, T2>(Document document, string transactionName, T0 param0, T1 param1, T2 param2, Action<Document, T0, T1, T2> action)
```

### <a id="Options">Options</a>

**FamilyLoadOptions**

Provide the callback for family load options. Shows a TaskDialog when loading a family that is different from what is loaded in the project.

```c#
document.LoadFamily(fileName, new FamilyLoadOptions(), out var family);
```

## Technology Sponsors

Thanks to [JetBrains](https://jetbrains.com) for providing licenses for [Rider](https://jetbrains.com/rider) and [dotUltimate](https://www.jetbrains.com/dotnet/) tools, which both
make open-source development a real pleasure!