using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;
using TUnit.Assertions.Enums;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class AsyncExternalEventCoreTests : RevitApiUiTest
{
    [Test]
    [Arguments(nameof(ExternalEventRequest.Denied))]
    [Arguments(nameof(ExternalEventRequest.TimedOut))]
    public async Task RaiseAsync_RefusedRequest_FaultsTheTaskWithoutExecution(string refusalName)
    {
        // Arrange
        var refusal = (ExternalEventRequest)Enum.Parse(typeof(ExternalEventRequest), refusalName);
        var executionCount = 0;
        var core = new AsyncExternalEventCore<int, int>((_, argument, _) => argument + executionCount++, () => refusal);

        // Act
        var raiseTask = core.RaiseAsync(42, CancellationToken.None);
        core.Execute(null!);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(() => raiseTask).Throws<InvalidOperationException>();
            await Assert.That(executionCount).IsZero();
        }
    }

    [Test]
    public async Task Execute_RefusedRaiseForTheRest_ExecutesTheRestInTheSameExecution()
    {
        // Arrange
        var raiseResults = new Queue<ExternalEventRequest>([ExternalEventRequest.Accepted, ExternalEventRequest.Pending, ExternalEventRequest.Pending, ExternalEventRequest.Denied, ExternalEventRequest.Denied]);
        var core = new AsyncExternalEventCore<int, int>((_, argument, _) => argument, raiseResults.Dequeue);
        var firstTask = core.RaiseAsync(1, CancellationToken.None);
        var secondTask = core.RaiseAsync(2, CancellationToken.None);
        var thirdTask = core.RaiseAsync(3, CancellationToken.None);

        // Act
        core.Execute(null!);

        // Assert
        var results = await Task.WhenAll(firstTask, secondTask, thirdTask);
        await Assert.That(results).IsEquivalentTo([1, 2, 3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Execute_AcceptedRaiseForTheRest_ExecutesTheOldestRaise()
    {
        // Arrange
        var raiseCount = 0;
        var core = new AsyncExternalEventCore<int, int>((_, argument, _) => argument, () =>
        {
            raiseCount++;
            return ExternalEventRequest.Accepted;
        });

        var firstTask = core.RaiseAsync(1, CancellationToken.None);
        var secondTask = core.RaiseAsync(2, CancellationToken.None);

        // Act
        core.Execute(null!);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(firstTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
            await Assert.That(secondTask.IsCompleted).IsFalse();
            await Assert.That(raiseCount).IsEqualTo(3);
        }
    }
}
