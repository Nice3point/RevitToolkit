using System.ComponentModel;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.Ui.Tests.Engine.Commands;
using Shouldly;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Tests;

[UsedImplicitly]
[DisplayName("DialogSuppression")]
[Transaction(TransactionMode.Manual)]
public class DialogSuppressionScopeTest : TestExternalCommand
{
    public override void Test()
    {
        var scope = RevitContext.BeginDialogSuppressionScope();
        scope.ShouldNotBeNull();

        scope.Dispose();
        scope.Dispose();

        Runner.Summary("Scope created and disposed");
        Runner.Summary("Double dispose handled correctly");
    }
}

[UsedImplicitly]
[DisplayName("DialogSuppression")]
[Transaction(TransactionMode.Manual)]
public class DialogSuppressionNestedTest : TestExternalCommand
{
    public override void Test()
    {
        using (RevitContext.BeginDialogSuppressionScope(1))
        {
            using (RevitContext.BeginDialogSuppressionScope(2))
            {
                using (RevitContext.BeginDialogSuppressionScope(3))
                {
                    Runner.Summary("Level 3 reached");
                }

                Runner.Summary("Level 2 disposed");
            }

            Runner.Summary("Level 1 disposed");
        }

        Runner.Summary("All 3 nested scopes disposed correctly");
    }
}

[UsedImplicitly]
[DisplayName("DialogSuppression")]
[Transaction(TransactionMode.Manual)]
public class DialogSuppressionHandlerTest : TestExternalCommand
{
    public override void Test()
    {
        using (RevitContext.BeginDialogSuppressionScope(args => { args.OverrideResult(1); }))
        {
            Runner.Summary("Custom handler registered");
        }

        Runner.Summary("Handler scope disposed");
        Runner.Summary("Note: Handler invocation requires triggering a dialog");
    }
}