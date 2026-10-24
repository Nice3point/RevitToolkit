using System.Diagnostics.CodeAnalysis;
using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;
using TUnit.Assertions.Enums;

namespace Nice3point.Revit.Toolkit.Tests;

[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
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

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_CanceledToken_ReturnsCanceledTaskWithoutExecution(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedArguments = new List<int>();
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument =>
        {
            receivedArguments.Add(argument);
            return argument;
        });

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        // Act
        var canceledTask = externalEvent.RaiseAsync(1, cancellationSource.Token);
        var followingResult = await externalEvent.RaiseAsync(2).WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(canceledTask.IsCanceled).IsTrue();
            await Assert.That(followingResult).IsEqualTo(2);
            await Assert.That(receivedArguments).IsEquivalentTo([2]);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_TokenCanceledBeforeTheEventCycle_CancelsTheTaskAtOnceAndExecutesTheFollowingRaise(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedArguments = new List<int>();
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument =>
        {
            receivedArguments.Add(argument);
            return argument;
        });

        using var cancellationSource = new CancellationTokenSource();

        // Act
        var canceledTask = externalEvent.RaiseAsync(1, cancellationSource.Token);
        var followingTask = externalEvent.RaiseAsync(2);
        cancellationSource.Cancel();
        var isCanceledOnCancel = canceledTask.IsCanceled;
        var followingResult = await followingTask.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isCanceledOnCancel).IsTrue();
            await Assert.That(followingResult).IsEqualTo(2);
            await Assert.That(receivedArguments).IsEquivalentTo([2]);
        }
    }

    [Test]
    public async Task RaiseAsync_TokenCanceledBeforeTheEventCycle_CancelsTheTaskWithThatToken()
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument => argument);
        using var cancellationSource = new CancellationTokenSource();

        // Act
        var canceledTask = externalEvent.RaiseAsync(1, cancellationSource.Token);
        cancellationSource.Cancel();

        // Assert
        var exception = await Assert.That(() => canceledTask).Throws<TaskCanceledException>();
        await Assert.That(exception!.CancellationToken).IsEqualTo(cancellationSource.Token);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_TokenCanceledWhileTheHandlerExecutes_CompletesWithTheHandlerResult(CancellationToken cancellationToken)
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument =>
        {
            // ReSharper disable once AccessToDisposedClosure
            cancellationSource.Cancel();
            return argument;
        });

        // Act
        var result = await externalEvent.RaiseAsync(42, cancellationSource.Token).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_TokenHandler_ReceivesTheTokenOfTheRaise(CancellationToken cancellationToken)
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var externalEvent = new AsyncRequestExternalEvent<int, CancellationToken>((_, _, raiseToken) => raiseToken);

        // Act
        var receivedToken = await externalEvent.RaiseAsync(42, cancellationSource.Token).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(receivedToken).IsEqualTo(cancellationSource.Token);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_TokenHandlerThrowsForItsToken_CancelsTheTask(CancellationToken cancellationToken)
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var externalEvent = new AsyncRequestExternalEvent<int, int>((_, argument, raiseToken) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            cancellationSource.Cancel();
            raiseToken.ThrowIfCancellationRequested();
            return argument;
        });

        // Act
        var raiseTask = externalEvent.RaiseAsync(42, cancellationSource.Token);

        // Assert
        var exception = await Assert.That(() => raiseTask.WaitAsync(cancellationToken)).Throws<TaskCanceledException>();
        await Assert.That(exception!.CancellationToken).IsEqualTo(cancellationSource.Token);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_HandlerThrowsForAnotherToken_FaultsTheTask(CancellationToken cancellationToken)
    {
        // Arrange
        using var raiseCancellationSource = new CancellationTokenSource();
        using var otherCancellationSource = new CancellationTokenSource();
        otherCancellationSource.Cancel();
        // ReSharper disable once AccessToDisposedClosure
        var externalEvent = new AsyncRequestExternalEvent<int, int>(_ => throw new OperationCanceledException(otherCancellationSource.Token));

        // Act
        var raiseTask = externalEvent.RaiseAsync(42, raiseCancellationSource.Token);

        // Assert
        await Assert.That(() => raiseTask.WaitAsync(cancellationToken)).Throws<OperationCanceledException>();
        await Assert.That(raiseTask.IsFaulted).IsTrue();
    }

    [Test]
    public async Task RaiseAsync_AllowDirectInvocationCanceledToken_ReturnsCanceledTaskWithoutExecution()
    {
        // Arrange
        var executionCount = 0;
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument => argument + executionCount++, ExternalEventOptions.AllowDirectInvocation);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        // Act
        var raiseTask = externalEvent.RaiseAsync(42, cancellationSource.Token);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(raiseTask.IsCanceled).IsTrue();
            await Assert.That(executionCount).IsZero();
        }
    }

    [Test]
    public async Task RaiseAsync_AllowDirectInvocationTokenCanceledWhileTheHandlerExecutes_CompletesWithTheHandlerResult()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument =>
        {
            // ReSharper disable once AccessToDisposedClosure
            cancellationSource.Cancel();
            return argument;
        }, ExternalEventOptions.AllowDirectInvocation);

        // Act
        var raiseTask = externalEvent.RaiseAsync(42, cancellationSource.Token);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(raiseTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
            await Assert.That(await raiseTask).IsEqualTo(42);
        }
    }

    [Test]
    public async Task RaiseAsync_AllowDirectInvocationTokenHandlerThrowsForItsToken_ReturnsCanceledTask()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var externalEvent = new AsyncRequestExternalEvent<int, int>((_, argument, raiseToken) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            cancellationSource.Cancel();
            raiseToken.ThrowIfCancellationRequested();
            return argument;
        }, ExternalEventOptions.AllowDirectInvocation);

        // Act
        var raiseTask = externalEvent.RaiseAsync(42, cancellationSource.Token);

        // Assert
        await Assert.That(raiseTask.IsCanceled).IsTrue();
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_TokenUiApplicationHandler_ReceivesTheUiApplicationAndToken(CancellationToken cancellationToken)
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var externalEvent = new AsyncRequestExternalEvent<(IntPtr MainWindowHandle, CancellationToken Token)>((uiApplication, raiseToken) => (uiApplication.MainWindowHandle, raiseToken));

        // Act
        var (mainWindowHandle, receivedToken) = await externalEvent.RaiseAsync(cancellationSource.Token).WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(mainWindowHandle).IsEqualTo(UiApplication.MainWindowHandle);
            await Assert.That(receivedToken).IsEqualTo(cancellationSource.Token);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_TokenCanceledBeforeTheEventCycle_CancelsTheTaskWithoutExecution(CancellationToken cancellationToken)
    {
        // Arrange
        var executionCount = 0;
        var externalEvent = new AsyncRequestExternalEvent<int>(() => ++executionCount);
        using var cancellationSource = new CancellationTokenSource();

        // Act
        var canceledTask = externalEvent.RaiseAsync(cancellationSource.Token);
        var followingTask = externalEvent.RaiseAsync();
        cancellationSource.Cancel();
        var followingResult = await followingTask.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(canceledTask.IsCanceled).IsTrue();
            await Assert.That(followingResult).IsEqualTo(1);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task Raise_AllowDirectInvocation_QueuesEveryCallWithoutWaiting(CancellationToken cancellationToken)
    {
        // Arrange
        var receivedTokens = new List<CancellationToken>();
        var execution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ExternalEventHandler externalEvent = new AsyncRequestExternalEvent<int>((_, token) =>
        {
            receivedTokens.Add(token);
            if (receivedTokens.Count == 2)
            {
                execution.SetResult();
            }

            return receivedTokens.Count;
        }, ExternalEventOptions.AllowDirectInvocation);

        // Act
        var firstRequest = externalEvent.Raise();
        var secondRequest = externalEvent.Raise();
        var executionCountOnRaise = receivedTokens.Count;
        await execution.Task.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(firstRequest).IsEqualTo(Autodesk.Revit.UI.ExternalEventRequest.Accepted);
            await Assert.That(secondRequest).IsEqualTo(Autodesk.Revit.UI.ExternalEventRequest.Pending);
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
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument =>
        {
            receivedArguments.Add(argument);
            return argument;
        });

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
            await Assert.That(firstRequest).IsEqualTo(Autodesk.Revit.UI.ExternalEventRequest.Accepted);
            await Assert.That(await firstTask).IsEqualTo(1);
            await Assert.That(await secondTask).IsEqualTo(2);
            await Assert.That(await lastTask).IsEqualTo(3);
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
        var externalEvent = new AsyncRequestExternalEvent<int, int>(argument =>
        {
            receivedArguments.Add(argument);
            if (receivedArguments.Count == 2)
            {
                execution.SetResult();
            }

            return argument;
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
