using System.ComponentModel;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using Nice3point.Revit.Toolkit.Ui.Tests.Engine.Commands;
using Shouldly;
using ExternalEvent = Nice3point.Revit.Toolkit.External.ExternalEvent;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Tests;

[UsedImplicitly]
[DisplayName("EventHandler")]
[Transaction(TransactionMode.Manual)]
public class ActionEventHandlerTest : TestExternalCommand
{
    public override void Test()
    {
        var executed = false;
        string? documentTitle = null;

        var externalEvent = new ExternalEvent(app =>
        {
            executed = true;
            documentTitle = app.ActiveUIDocument?.Document.Title;
        });

        externalEvent.Raise();

        executed.ShouldBeTrue("Handler should execute synchronously in API mode");
        documentTitle.ShouldNotBeNullOrEmpty();

        Runner.Summary($"Document: {documentTitle}");
        Runner.Summary($"IsApiMode: {RevitContext.IsRevitInApiMode}");
    }
}

[UsedImplicitly]
[DisplayName("EventHandler")]
[Transaction(TransactionMode.Manual)]
public class AsyncEventHandlerTest : AsyncTestExternalCommand
{
    public override async Task TestAsync()
    {
        var executed = false;
        string? documentTitle = null;

        var externalEvent = new AsyncExternalEvent(app =>
        {
            executed = true;
            documentTitle = app.ActiveUIDocument?.Document.Title;
        });

        await externalEvent.RaiseAsync();

        executed.ShouldBeTrue();
        documentTitle.ShouldNotBeNullOrEmpty();

        Runner.Summary($"Document: {documentTitle}");
    }
}

[UsedImplicitly]
[DisplayName("EventHandler")]
[Transaction(TransactionMode.Manual)]
public class AsyncEventHandlerGenericTest : AsyncTestExternalCommand
{
    public override async Task TestAsync()
    {
        var externalEvent = new AsyncRequestExternalEvent<DocumentInfo>(app =>
        {
            var doc = app.ActiveUIDocument?.Document;
            if (doc is null) return new DocumentInfo("null", 0);

            var count = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .GetElementCount();

            return new DocumentInfo(doc.Title, count);
        });

        var result = await externalEvent.RaiseAsync();

        result.ShouldNotBeNull();
        result.Title.ShouldNotBeNullOrEmpty();
        result.ElementCount.ShouldBeGreaterThanOrEqualTo(0);

        Runner.Summary($"Document: {result.Title}");
        Runner.Summary($"Elements: {result.ElementCount}");
    }

    private sealed record DocumentInfo(string Title, int ElementCount);
}