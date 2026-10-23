using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;
using TUnit.Assertions.Enums;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class AsyncRequestExternalEventTests : RevitApiUiTest
{
    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_Handler_ReturnsTheHandlerResult(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int>(() => 42);

        // Act
        var raiseTask = externalEvent.RaiseAsync();
        var isCompletedOnRaise = raiseTask.IsCompleted;
        var result = await raiseTask.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isCompletedOnRaise).IsFalse();
            await Assert.That(result).IsEqualTo(42);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_UiApplicationHandler_ReturnsTheUiApplicationResult(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<IntPtr>(uiApplication => uiApplication.MainWindowHandle);

        // Act
        var result = await externalEvent.RaiseAsync().WaitAsync(cancellationToken);

        // Assert
        await Assert.That(result).IsEqualTo(UiApplication.MainWindowHandle);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_HandlerThrows_FaultsTheTask(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int>(() => throw new InvalidOperationException("Handler failure"));

        // Act & Assert
        await Assert.That(() => externalEvent.RaiseAsync().WaitAsync(cancellationToken))
            .Throws<InvalidOperationException>()
            .WithMessage("Handler failure");
    }

    [Test]
    public async Task RaiseAsync_AllowDirectInvocation_CompletesOnRaiseWithTheResult()
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int>(() => 42, ExternalEventOptions.AllowDirectInvocation);

        // Act
        var raiseTask = externalEvent.RaiseAsync();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(raiseTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
            await Assert.That(await raiseTask).IsEqualTo(42);
        }
    }

    [Test]
    public async Task RaiseAsync_AllowDirectInvocationHandlerThrows_ReturnsFaultedTask()
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int>(() => throw new InvalidOperationException("Handler failure"), ExternalEventOptions.AllowDirectInvocation);

        // Act
        var raiseTask = externalEvent.RaiseAsync();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(raiseTask.IsFaulted).IsTrue();
            await Assert.That(raiseTask.Exception!.InnerException).IsTypeOf<InvalidOperationException>();
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_ArgumentHandler_ReturnsTheResultForTheArgument(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int, string>(argument => $"Result {argument}");

        // Act
        var result = await externalEvent.RaiseAsync(42).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(result).IsEqualTo("Result 42");
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_ArgumentUiApplicationHandler_ReceivesTheUiApplicationAndArgument(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int, (IntPtr MainWindowHandle, int Argument)>((uiApplication, payload) => (uiApplication.MainWindowHandle, payload));

        // Act
        var (mainWindowHandle, argument) = await externalEvent.RaiseAsync(42).WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(mainWindowHandle).IsEqualTo(UiApplication.MainWindowHandle);
            await Assert.That(argument).IsEqualTo(42);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_ArgumentHandlerThrows_FaultsTheTask(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int, string>(_ => throw new InvalidOperationException("Handler failure"));

        // Act & Assert
        await Assert.That(Task () => externalEvent.RaiseAsync(42).WaitAsync(cancellationToken))
            .Throws<InvalidOperationException>()
            .WithMessage("Handler failure");
    }

    [Test]
    public async Task RaiseAsync_ArgumentAllowDirectInvocation_CompletesOnRaiseWithTheResult()
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument => argument * 2, ExternalEventOptions.AllowDirectInvocation);

        // Act
        var raiseTask = externalEvent.RaiseAsync(21);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(raiseTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
            await Assert.That(await raiseTask).IsEqualTo(42);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_RaisesBeforeTheEventCycle_CompletesEachTaskWithItsOwnResult(CancellationToken cancellationToken)
    {
        // Arrange
        var executionCount = 0;
        var externalEvent = new AsyncRequestExternalEvent<int>(() => ++executionCount);

        // Act
        var firstTask = externalEvent.RaiseAsync();
        var secondTask = externalEvent.RaiseAsync();
        var results = await Task.WhenAll(firstTask, secondTask).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(results).IsEquivalentTo([1, 2], CollectionOrdering.Matching);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_ArgumentRaisesBeforeTheEventCycle_CompletesEachTaskWithTheResultOfItsArgument(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedArguments = new List<int>();
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument =>
        {
            receivedArguments.Add(argument);
            return argument;
        });

        // Act
        var firstTask = externalEvent.RaiseAsync(1);
        var secondTask = externalEvent.RaiseAsync(2);
        var results = await Task.WhenAll(firstTask, secondTask).WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(results).IsEquivalentTo([1, 2], CollectionOrdering.Matching);
            await Assert.That(receivedArguments).IsEquivalentTo([1, 2], CollectionOrdering.Matching);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_ArgumentFirstRaiseThrows_CompletesTheFollowingRaiseWithItsResult(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument => argument == 1 ? throw new InvalidOperationException("Handler failure") : argument);

        // Act
        var failingTask = externalEvent.RaiseAsync(1);
        var followingTask = externalEvent.RaiseAsync(2);
        var followingResult = await followingTask.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(() => failingTask.WaitAsync(cancellationToken)).Throws<InvalidOperationException>();
            await Assert.That(followingResult).IsEqualTo(2);
        }
    }
}
