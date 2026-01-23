using System.ComponentModel;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.Ui.Tests.Engine;
using Nice3point.Revit.Toolkit.Ui.Tests.Engine.Commands;
using Shouldly;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Tests;

[UsedImplicitly]
[DisplayName("FailureSuppression")]
[Transaction(TransactionMode.Manual)]
public class FailureSuppressionResolveTest : TestExternalCommand
{
    public override void Test()
    {
        if (!FamilyDocumentHelper.RequireFamilyDocument(ActiveDocument)) return;

        using (RevitApiContext.BeginFailureSuppressionScope(resolveErrors: true))
        {
            using var transaction = new Transaction(ActiveDocument, "Test Resolve");
            transaction.Start();

            FamilyDocumentHelper.CreateModelLine(ActiveDocument, 10);
            FamilyDocumentHelper.CreateModelLine(ActiveDocument, 10);

            var status = transaction.Commit();

            status.ShouldBe(TransactionStatus.Committed);

            Runner.Summary($"Transaction status: {status}");
            Runner.Summary("resolveErrors: true");
            Runner.Summary("Overlapping lines created to trigger warnings");
        }
    }
}

[UsedImplicitly]
[DisplayName("FailureSuppression")]
[Transaction(TransactionMode.Manual)]
public class FailureSuppressionDismissTest : TestExternalCommand
{
    public override void Test()
    {
        if (!FamilyDocumentHelper.RequireFamilyDocument(ActiveDocument)) return;

        using (RevitApiContext.BeginFailureSuppressionScope(resolveErrors: false))
        {
            using var transaction = new Transaction(ActiveDocument, "Test Dismiss");
            transaction.Start();

            FamilyDocumentHelper.CreateModelLine(ActiveDocument, 10);
            FamilyDocumentHelper.CreateModelLine(ActiveDocument, 10);

            var status = transaction.Commit();

            status.ShouldBeOneOf(TransactionStatus.Committed, TransactionStatus.RolledBack);

            Runner.Summary($"Transaction status: {status}");
            Runner.Summary("resolveErrors: false");
        }
    }
}

[UsedImplicitly]
[DisplayName("FailureSuppression")]
[Transaction(TransactionMode.Manual)]
public class FailureSuppressionNestedTest : TestExternalCommand
{
    public override void Test()
    {
        if (!FamilyDocumentHelper.RequireFamilyDocument(ActiveDocument)) return;

        using (RevitApiContext.BeginFailureSuppressionScope())
        {
            using (RevitApiContext.BeginFailureSuppressionScope())
            {
                using var transaction = new Transaction(ActiveDocument, "Nested Test");
                transaction.Start();

                FamilyDocumentHelper.CreateModelLine(ActiveDocument, 10);

                var status = transaction.Commit();

                status.ShouldBe(TransactionStatus.Committed);

                Runner.Summary($"Transaction status: {status}");
                Runner.Summary("Nesting level: 2");
            }
        }
    }
}

[UsedImplicitly]
[DisplayName("FailureSuppression")]
[Transaction(TransactionMode.Manual)]
public class FailureSuppressionCombinedTest : TestExternalCommand
{
    public override void Test()
    {
        if (!FamilyDocumentHelper.RequireFamilyDocument(ActiveDocument)) return;

        using (RevitApiContext.BeginFailureSuppressionScope())
        using (RevitContext.BeginDialogSuppressionScope())
        {
            using var transaction = new Transaction(ActiveDocument, "Combined Test");
            transaction.Start();

            FamilyDocumentHelper.CreateModelLine(ActiveDocument, 10);
            FamilyDocumentHelper.CreateModelLine(ActiveDocument, 10);

            var status = transaction.Commit();

            status.ShouldBe(TransactionStatus.Committed);

            Runner.Summary($"Transaction status: {status}");
            Runner.Summary("FailureSuppression: active");
            Runner.Summary("DialogSuppression: active");
        }
    }
}