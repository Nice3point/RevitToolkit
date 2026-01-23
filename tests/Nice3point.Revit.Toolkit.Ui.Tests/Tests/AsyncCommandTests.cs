using System.ComponentModel;
using System.Net.Http;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.Ui.Tests.Engine;
using Nice3point.Revit.Toolkit.Ui.Tests.Engine.Commands;
using Shouldly;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Tests;

[UsedImplicitly]
[DisplayName("AsyncCommand")]
[Transaction(TransactionMode.Manual)]
public class AsyncCommandDelayTest : AsyncTestExternalCommand
{
    public override async Task TestAsync()
    {
        const int delayMs = 1000;

        var titleBefore = ActiveDocument.Title;
        var start = DateTime.Now;

        await Task.Delay(delayMs);

        var elapsed = (DateTime.Now - start).TotalMilliseconds;
        var titleAfter = ActiveDocument.Title;

        elapsed.ShouldBeGreaterThanOrEqualTo(delayMs);
        titleAfter.ShouldNotBeNullOrEmpty();
        titleAfter.ShouldBe(titleBefore);

        Runner.Summary($"Elapsed: {elapsed:F0}ms");
        Runner.Summary($"Document before: {titleBefore}");
        Runner.Summary($"Document after: {titleAfter}");
    }
}

[UsedImplicitly]
[DisplayName("AsyncCommand")]
[Transaction(TransactionMode.Manual)]
public class AsyncCommandHttpTest : AsyncTestExternalCommand
{
    public override async Task TestAsync()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = await client.GetStringAsync("https://httpbin.org/get");

        response.ShouldNotBeNullOrEmpty();

        var titleAfter = ActiveDocument.Title;
        titleAfter.ShouldNotBeNullOrEmpty();

        Runner.Summary($"Response length: {response.Length} chars");
        Runner.Summary($"Document accessible after HTTP: {titleAfter}");
    }
}

[UsedImplicitly]
[DisplayName("AsyncCommand")]
[Transaction(TransactionMode.Manual)]
public class AsyncCommandTransactionTest : AsyncTestExternalCommand
{
    public override async Task TestAsync()
    {
        if (!FamilyDocumentHelper.RequireFamilyDocument(ActiveDocument)) return;

        await Task.Delay(500);

        using (RevitApiContext.BeginFailureSuppressionScope())
        {
            using var transaction = new Transaction(ActiveDocument, "Async Transaction");
            transaction.Start();

            FamilyDocumentHelper.CreateModelLine(ActiveDocument, 5);

            var status = transaction.Commit();

            status.ShouldBe(TransactionStatus.Committed);

            Runner.Summary($"Transaction status: {status}");
            Runner.Summary("Transaction created after Task.Delay");
        }
    }
}