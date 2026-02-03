using System.ComponentModel;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using Nice3point.Revit.Toolkit.Ui.Tests.Engine.Commands;
using Shouldly;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Tests;

[UsedImplicitly]
[DisplayName("EventHandler")]
[Transaction(TransactionMode.Manual)]
public class ActionEventHandlerTest : TestExternalCommand
{
    private static readonly ActionEventHandler Handler = new();

    public override void Test()
    {
        var executed = false;
        string? documentTitle = null;

        Handler.Raise(app =>
        {
            executed = true;
            documentTitle = app.ActiveUIDocument?.Document.Title;
        });

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
    private static readonly AsyncEventHandler Handler = new();

    public override async Task TestAsync()
    {
        var executed = false;
        string? documentTitle = null;

        await Handler.RaiseAsync(app =>
        {
            executed = true;
            documentTitle = app.ActiveUIDocument?.Document.Title;
        });

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
    private static readonly AsyncEventHandler<DocumentInfo> Handler = new();

    public override async Task TestAsync()
    {
        var result = await Handler.RaiseAsync(app =>
        {
            var doc = app.ActiveUIDocument?.Document;
            if (doc is null) return new DocumentInfo("null", 0);

            var count = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .GetElementCount();

            return new DocumentInfo(doc.Title, count);
        });

        result.ShouldNotBeNull();
        result.Title.ShouldNotBeNullOrEmpty();
        result.ElementCount.ShouldBeGreaterThanOrEqualTo(0);

        Runner.Summary($"Document: {result.Title}");
        Runner.Summary($"Elements: {result.ElementCount}");
    }

    private sealed record DocumentInfo(string Title, int ElementCount);
}

[UsedImplicitly]
[DisplayName("EventHandler")]
[Transaction(TransactionMode.Manual)]
public class IdlingEventHandlerTest : TestExternalCommand
{
    private static readonly IdlingEventHandler Handler = new();

    public override void Test()
    {
        Handler.Raise(app =>
        {
            var title = app.ActiveUIDocument?.Document.Title;

            new TaskDialog("IdlingEventHandler Executed")
            {
                MainContent = $"Handler executed on Idling!\n\nDocument: {title}\nTime: {DateTime.Now:HH:mm:ss.fff}",
                MainIcon = TaskDialogIcon.TaskDialogIconShield
            }.Show();
        });

        Runner.Summary("Handler queued for Idling event");
        Runner.Summary("A dialog will appear when executed");
    }
}