using System.ComponentModel;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.Ui.Tests.Engine.Commands;
using Shouldly;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Tests;

[UsedImplicitly]
[DisplayName("RevitContext")]
[Transaction(TransactionMode.Manual)]
public class RevitContextPropertiesTest : TestExternalCommand
{
    public override void Test()
    {
        var document = RevitContext.ActiveDocument;
        var uiDocument = RevitContext.ActiveUiDocument;
        var activeView = RevitContext.ActiveView;

        document.ShouldNotBeNull();
        document.IsValidObject.ShouldBeTrue();

        uiDocument.ShouldNotBeNull();
        uiDocument.Document.IsValidObject.ShouldBeTrue();

        document.Title.ShouldBe(uiDocument.Document.Title);

        activeView.ShouldNotBeNull();
        activeView.IsValidObject.ShouldBeTrue();

        Runner.Summary($"Document: {document.Title}");
        Runner.Summary($"ActiveView: {activeView.Name} ({activeView.ViewType})");
        Runner.Summary($"GraphicalView: {RevitContext.ActiveGraphicalView?.Name ?? "null"}");
    }
}

[UsedImplicitly]
[DisplayName("RevitContext")]
[Transaction(TransactionMode.Manual)]
public class RevitContextInApiModeTest : TestExternalCommand
{
    public override void Test()
    {
        var isApiMode = RevitContext.IsRevitInApiMode;

        isApiMode.ShouldBeTrue("Should be in API mode when executing ExternalCommand");

        Runner.Summary($"IsRevitInApiMode: {isApiMode}");
    }
}

[UsedImplicitly]
[DisplayName("RevitContext")]
[Transaction(TransactionMode.Manual)]
public class RevitContextOutApiModeTest : AsyncTestExternalCommand
{
    public override async Task TestAsync()
    {
        var isApiMode = await Task.Run(() => RevitContext.IsRevitInApiMode);

        isApiMode.ShouldBeFalse("Should be outside the API mode when executing in a different Thread");

        Runner.Summary($"IsRevitInApiMode: {isApiMode}");
    }
}