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

    [Test]
    [Arguments(nameof(ExternalEventRequest.Denied))]
    [Arguments(nameof(ExternalEventRequest.TimedOut))]
    public async Task Raise_RefusedRequest_ReturnsTheRefusalWithoutExecution(string refusalName)
    {
        // Arrange
        var refusal = (ExternalEventRequest)Enum.Parse(typeof(ExternalEventRequest), refusalName);
        var executionCount = 0;
        var core = new AsyncExternalEventCore<int, int>((_, argument, _) => argument + executionCount++, () => refusal);

        // Act
        var request = core.Raise(42);
        core.Execute(null!);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(request).IsEqualTo(refusal);
            await Assert.That(executionCount).IsZero();
        }
    }

    [Test]
    public async Task Raise_BetweenAsyncRaises_PreservesTheirResultsAndExecutionOrder()
    {
        // Arrange
        var receivedArguments = new List<int>();
        var receivedTokens = new List<CancellationToken>();
        using var cancellationSource = new CancellationTokenSource();
        var core = new AsyncExternalEventCore<int, int>((_, argument, token) =>
        {
            receivedArguments.Add(argument);
            receivedTokens.Add(token);
            return argument;
        }, () => ExternalEventRequest.Accepted);

        // Act
        var firstTask = core.RaiseAsync(1, cancellationSource.Token);
        var request = core.Raise(2);
        var lastTask = core.RaiseAsync(3, cancellationSource.Token);
        core.Execute(null!);
        core.Execute(null!);
        var isLastTaskCompletedAfterSecondExecution = lastTask.IsCompleted;
        core.Execute(null!);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(request).IsEqualTo(ExternalEventRequest.Accepted);
            await Assert.That(await firstTask).IsEqualTo(1);
            await Assert.That(await lastTask).IsEqualTo(3);
            await Assert.That(isLastTaskCompletedAfterSecondExecution).IsFalse();
            await Assert.That(receivedArguments).IsEquivalentTo([1, 2, 3], CollectionOrdering.Matching);
            await Assert.That(receivedTokens).IsEquivalentTo([cancellationSource.Token, CancellationToken.None, cancellationSource.Token], CollectionOrdering.Matching);
        }
    }

    [Test]
    public async Task Execute_RaiseHandlerThrows_PropagatesTheExceptionAndSchedulesTheFollowingRaise()
    {
        // Arrange
        var failure = new InvalidOperationException("Handler failure");
        var raiseCount = 0;
        var core = new AsyncExternalEventCore<int, int>((_, argument, _) => argument == 1 ? throw failure : argument, () =>
        {
            raiseCount++;
            return ExternalEventRequest.Accepted;
        });

        core.Raise(1);
        var followingTask = core.RaiseAsync(2, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.That(() => core.Execute(null!)).Throws<InvalidOperationException>();
        await Assert.That(exception).IsSameReferenceAs(failure);
        await Assert.That(raiseCount).IsEqualTo(3);
        await Assert.That(followingTask.IsCompleted).IsFalse();

        core.Execute(null!);
        await Assert.That(await followingTask).IsEqualTo(2);
    }
}
