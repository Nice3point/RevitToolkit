using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;
using TUnit.Assertions.Enums;

namespace Nice3point.Revit.Toolkit.Tests;

// ReSharper disable MethodSupportsCancellation
public sealed class AsyncExternalEventTests : RevitApiUiTest
{
    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_Handler_CompletesAfterTheHandlerExecutes(CancellationToken cancellationToken)
    {
        // Arrange
        var executionCount = 0;
        var externalEvent = new AsyncExternalEvent(() => executionCount++);

        // Act
        var raiseTask = externalEvent.RaiseAsync();
        var isCompletedOnRaise = raiseTask.IsCompleted;
        await raiseTask.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isCompletedOnRaise).IsFalse();
            await Assert.That(executionCount).IsEqualTo(1);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_UiApplicationHandler_ReceivesTheUiApplication(CancellationToken cancellationToken)
    {
        // Arrange
        UIApplication? receivedApplication = null;
        var externalEvent = new AsyncExternalEvent(uiApplication => receivedApplication = uiApplication);

        // Act
        await externalEvent.RaiseAsync().WaitAsync(cancellationToken);

        // Assert
        await Assert.That(receivedApplication!.MainWindowHandle).IsEqualTo(UiApplication.MainWindowHandle);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_HandlerThrows_FaultsTheTask(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncExternalEvent(() => throw new InvalidOperationException("Handler failure"));

        // Act & Assert
        await Assert.That(() => externalEvent.RaiseAsync().WaitAsync(cancellationToken))
            .Throws<InvalidOperationException>()
            .WithMessage("Handler failure");
    }

    [Test]
    public async Task RaiseAsync_AllowDirectInvocation_CompletesOnRaise()
    {
        // Arrange
        var executionCount = 0;
        var externalEvent = new AsyncExternalEvent(() => executionCount++, ExternalEventOptions.AllowDirectInvocation);

        // Act
        var raiseTask = externalEvent.RaiseAsync();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(raiseTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
            await Assert.That(executionCount).IsEqualTo(1);
        }
    }

    [Test]
    public async Task RaiseAsync_AllowDirectInvocationHandlerThrows_ReturnsFaultedTask()
    {
        // Arrange
        var externalEvent = new AsyncExternalEvent(() => throw new InvalidOperationException("Handler failure"), ExternalEventOptions.AllowDirectInvocation);

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
    public async Task RaiseAsync_GenericHandler_ReceivesTheArgument(CancellationToken cancellationToken)
    {
        // Arrange
        string? receivedArgument = null;
        var externalEvent = new AsyncExternalEvent<string>(argument => receivedArgument = argument);

        // Act
        await externalEvent.RaiseAsync("Queued argument").WaitAsync(cancellationToken);

        // Assert
        await Assert.That(receivedArgument).IsEqualTo("Queued argument");
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_GenericUiApplicationHandler_ReceivesTheUiApplicationAndArgument(CancellationToken cancellationToken)
    {
        // Arrange
        UIApplication? receivedApplication = null;
        var receivedArgument = 0;
        var externalEvent = new AsyncExternalEvent<int>((uiApplication, argument) =>
        {
            receivedApplication = uiApplication;
            receivedArgument = argument;
        });

        // Act
        await externalEvent.RaiseAsync(42).WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(receivedApplication!.MainWindowHandle).IsEqualTo(UiApplication.MainWindowHandle);
            await Assert.That(receivedArgument).IsEqualTo(42);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_GenericHandlerThrows_FaultsTheTask(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncExternalEvent<int>(_ => throw new InvalidOperationException("Handler failure"));

        // Act & Assert
        await Assert.That(() => externalEvent.RaiseAsync(42).WaitAsync(cancellationToken))
            .Throws<InvalidOperationException>()
            .WithMessage("Handler failure");
    }

    [Test]
    public async Task RaiseAsync_GenericAllowDirectInvocation_CompletesOnRaiseWithTheArgument()
    {
        // Arrange
        var receivedArguments = new List<int>();
        var externalEvent = new AsyncExternalEvent<int>(receivedArguments.Add, ExternalEventOptions.AllowDirectInvocation);

        // Act
        var firstTask = externalEvent.RaiseAsync(1);
        var secondTask = externalEvent.RaiseAsync(2);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(firstTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
            await Assert.That(secondTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
            await Assert.That(receivedArguments).IsEquivalentTo([1, 2]);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_SequentialRaises_ExecutesEveryRaise(CancellationToken cancellationToken)
    {
        // Arrange
        var executionCount = 0;
        var externalEvent = new AsyncExternalEvent(() => executionCount++);

        // Act
        await externalEvent.RaiseAsync().WaitAsync(cancellationToken);
        await externalEvent.RaiseAsync().WaitAsync(cancellationToken);
        await externalEvent.RaiseAsync().WaitAsync(cancellationToken);

        // Assert
        await Assert.That(executionCount).IsEqualTo(3);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_RaisesBeforeTheEventCycle_ExecutesTheHandlerForEveryRaise(CancellationToken cancellationToken)
    {
        // Arrange
        var executionCount = 0;
        var externalEvent = new AsyncExternalEvent(() => executionCount++);

        // Act
        var firstTask = externalEvent.RaiseAsync();
        var secondTask = externalEvent.RaiseAsync();
        await Task.WhenAll(firstTask, secondTask).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(executionCount).IsEqualTo(2);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_GenericRaisesBeforeTheEventCycle_ExecutesTheHandlerForEachArgumentInOrder(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedArguments = new List<int>();
        var externalEvent = new AsyncExternalEvent<int>(receivedArguments.Add);

        // Act
        var firstTask = externalEvent.RaiseAsync(1);
        var secondTask = externalEvent.RaiseAsync(2);
        var thirdTask = externalEvent.RaiseAsync(3);
        await Task.WhenAll(firstTask, secondTask, thirdTask).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(receivedArguments).IsEquivalentTo([1, 2, 3], CollectionOrdering.Matching);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_GenericFirstRaiseThrows_CompletesTheFollowingRaise(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedArguments = new List<int>();
        var externalEvent = new AsyncExternalEvent<int>(argument =>
        {
            receivedArguments.Add(argument);
            if (argument == 1)
            {
                throw new InvalidOperationException("Handler failure");
            }
        });

        // Act
        var failingTask = externalEvent.RaiseAsync(1);
        var followingTask = externalEvent.RaiseAsync(2);
        await followingTask.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(() => failingTask.WaitAsync(cancellationToken)).Throws<InvalidOperationException>();
            await Assert.That(followingTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
            await Assert.That(receivedArguments).IsEquivalentTo([1, 2], CollectionOrdering.Matching);
        }
    }

    [Test]
    public async Task RaiseAsync_CanceledToken_ReturnsCanceledTask()
    {
        // Arrange
        var externalEvent = new AsyncExternalEvent(() => { });
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        // Act
        var raiseTask = externalEvent.RaiseAsync(cancellationSource.Token);

        // Assert
        await Assert.That(raiseTask.IsCanceled).IsTrue();
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_TokenCanceledBeforeTheEventCycle_CancelsTheTaskWithoutExecution(CancellationToken cancellationToken)
    {
        // Arrange
        var executionCount = 0;
        var externalEvent = new AsyncExternalEvent(() => executionCount++);
        using var cancellationSource = new CancellationTokenSource();

        // Act
        var canceledTask = externalEvent.RaiseAsync(cancellationSource.Token);
        var followingTask = externalEvent.RaiseAsync();
        cancellationSource.Cancel();
        await followingTask.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(canceledTask.IsCanceled).IsTrue();
            await Assert.That(executionCount).IsEqualTo(1);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_TokenUiApplicationHandler_ReceivesTheUiApplicationAndToken(CancellationToken cancellationToken)
    {
        // Arrange
        UIApplication? receivedApplication = null;
        CancellationToken receivedToken = default;
        using var cancellationSource = new CancellationTokenSource();
        var externalEvent = new AsyncExternalEvent((uiApplication, raiseToken) =>
        {
            receivedApplication = uiApplication;
            receivedToken = raiseToken;
        });

        // Act
        await externalEvent.RaiseAsync(cancellationSource.Token).WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(receivedApplication!.MainWindowHandle).IsEqualTo(UiApplication.MainWindowHandle);
            await Assert.That(receivedToken).IsEqualTo(cancellationSource.Token);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_GenericTokenCanceledBeforeTheEventCycle_ExecutesOnlyTheFollowingRaise(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedArguments = new List<int>();
        var externalEvent = new AsyncExternalEvent<int>(receivedArguments.Add);
        using var cancellationSource = new CancellationTokenSource();

        // Act
        var canceledTask = externalEvent.RaiseAsync(1, cancellationSource.Token);
        var followingTask = externalEvent.RaiseAsync(2);
        cancellationSource.Cancel();
        await followingTask.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(canceledTask.IsCanceled).IsTrue();
            await Assert.That(receivedArguments).IsEquivalentTo([2]);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_GenericTokenHandler_ReceivesTheArgumentAndToken(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedArgument = 0;
        CancellationToken receivedToken = default;
        using var cancellationSource = new CancellationTokenSource();
        var externalEvent = new AsyncExternalEvent<int>((_, argument, raiseToken) =>
        {
            receivedArgument = argument;
            receivedToken = raiseToken;
        });

        // Act
        await externalEvent.RaiseAsync(42, cancellationSource.Token).WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(receivedArgument).IsEqualTo(42);
            await Assert.That(receivedToken).IsEqualTo(cancellationSource.Token);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task Raise_AllowDirectInvocation_QueuesEveryCallWithoutWaiting(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedTokens = new List<CancellationToken>();
        var execution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ExternalEventHandler externalEvent = new AsyncExternalEvent((_, token) =>
        {
            receivedTokens.Add(token);
            if (receivedTokens.Count == 2)
            {
                execution.SetResult();
            }
        }, ExternalEventOptions.AllowDirectInvocation);

        // Act
        var firstRequest = externalEvent.Raise();
        var secondRequest = externalEvent.Raise();
        var executionCountOnRaise = receivedTokens.Count;
        await execution.Task.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(firstRequest).IsEqualTo(ExternalEventRequest.Accepted);
            await Assert.That(secondRequest).IsEqualTo(ExternalEventRequest.Pending);
            await Assert.That(executionCountOnRaise).IsZero();
            await Assert.That(receivedTokens).IsEquivalentTo([CancellationToken.None, CancellationToken.None], CollectionOrdering.Matching);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task Raise_GenericMixedRaises_QueuesTheLastArgumentWithoutChangingAsyncCompletion(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedArguments = new List<int>();
        var externalEvent = new AsyncExternalEvent<int>(receivedArguments.Add);

        // Act
        var firstRequest = externalEvent.Raise();
        var firstTask = externalEvent.RaiseAsync(1);
        externalEvent.Raise();
        var secondTask = externalEvent.RaiseAsync(2);
        externalEvent.Raise();
        var lastTask = externalEvent.RaiseAsync(3);
        await Task.WhenAll(firstTask, secondTask, lastTask).WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(firstRequest).IsEqualTo(ExternalEventRequest.Accepted);
            await Assert.That(receivedArguments).IsEquivalentTo([0, 1, 1, 2, 2, 3], CollectionOrdering.Matching);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task Raise_GenericAfterDirectInvocation_QueuesTheLastArgument(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedArguments = new List<int>();
        var execution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var externalEvent = new AsyncExternalEvent<int>(argument =>
        {
            receivedArguments.Add(argument);
            if (receivedArguments.Count == 2)
            {
                execution.SetResult();
            }
        }, ExternalEventOptions.AllowDirectInvocation);

        await externalEvent.RaiseAsync(42).WaitAsync(cancellationToken);

        // Act
        externalEvent.Raise();
        var executionCountOnRaise = receivedArguments.Count;
        await execution.Task.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(executionCountOnRaise).IsEqualTo(1);
            await Assert.That(receivedArguments).IsEquivalentTo([42, 42], CollectionOrdering.Matching);
        }
    }
}
