using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;

namespace Nice3point.Revit.Toolkit.Tests;

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
            await Assert.That(raiseTask.IsCompletedSuccessfully).IsTrue();
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
            await Assert.That(firstTask.IsCompletedSuccessfully && secondTask.IsCompletedSuccessfully).IsTrue();
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
}
