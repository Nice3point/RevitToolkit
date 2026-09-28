using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;
using ExternalEvent = Nice3point.Revit.Toolkit.External.ExternalEvent;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class ExternalEventTests : RevitApiUiTest
{
    [Test]
    [Timeout(10_000)]
    public async Task Raise_Handler_ExecutesInTheNextEventCycle(CancellationToken cancellationToken)
    {
        // Arrange
        var execution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var externalEvent = new ExternalEvent(() => execution.SetResult());

        // Act
        var request = externalEvent.Raise();
        var isExecutedOnRaise = execution.Task.IsCompleted;
        await execution.Task.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(request).IsEqualTo(ExternalEventRequest.Accepted);
            await Assert.That(isExecutedOnRaise).IsFalse();
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task Raise_UiApplicationHandler_ReceivesTheUiApplication(CancellationToken cancellationToken)
    {
        // Arrange
        var execution = new TaskCompletionSource<UIApplication>(TaskCreationOptions.RunContinuationsAsynchronously);
        var externalEvent = new ExternalEvent(uiApplication => execution.SetResult(uiApplication));

        // Act
        externalEvent.Raise();
        var receivedApplication = await execution.Task.WaitAsync(cancellationToken);

        // Assert
        await Assert.That(receivedApplication.MainWindowHandle).IsEqualTo(UiApplication.MainWindowHandle);
    }

    [Test]
    public async Task Raise_AllowDirectInvocation_ExecutesOnRaise()
    {
        // Arrange
        var executionCount = 0;
        var externalEvent = new ExternalEvent(() => executionCount++, ExternalEventOptions.AllowDirectInvocation);

        // Act
        var request = externalEvent.Raise();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(request).IsEqualTo(ExternalEventRequest.Accepted);
            await Assert.That(executionCount).IsEqualTo(1);
        }
    }

    [Test]
    public async Task Raise_AllowDirectInvocationWithUiApplicationHandler_ReceivesTheUiApplication()
    {
        // Arrange
        UIApplication? receivedApplication = null;
        var externalEvent = new ExternalEvent(uiApplication => receivedApplication = uiApplication, ExternalEventOptions.AllowDirectInvocation);

        // Act
        externalEvent.Raise();

        // Assert
        await Assert.That(receivedApplication!.MainWindowHandle).IsEqualTo(UiApplication.MainWindowHandle);
    }

    [Test]
    [Timeout(10_000)]
    public async Task Raise_GenericHandler_ReceivesTheArgument(CancellationToken cancellationToken)
    {
        // Arrange
        var execution = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var externalEvent = new ExternalEvent<string>(argument => execution.SetResult(argument));

        // Act
        externalEvent.Raise("Queued argument");
        var receivedArgument = await execution.Task.WaitAsync(cancellationToken);

        // Assert
        await Assert.That(receivedArgument).IsEqualTo("Queued argument");
    }

    [Test]
    [Timeout(10_000)]
    public async Task Raise_GenericUiApplicationHandler_ReceivesTheUiApplicationAndArgument(CancellationToken cancellationToken)
    {
        // Arrange
        var execution = new TaskCompletionSource<(UIApplication UiApplication, int Argument)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var externalEvent = new ExternalEvent<int>((uiApplication, argument) => execution.SetResult((uiApplication, argument)));

        // Act
        externalEvent.Raise(42);
        var (receivedApplication, receivedArgument) = await execution.Task.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(receivedApplication.MainWindowHandle).IsEqualTo(UiApplication.MainWindowHandle);
            await Assert.That(receivedArgument).IsEqualTo(42);
        }
    }

    [Test]
    public async Task Raise_GenericAllowDirectInvocation_ExecutesOnRaiseWithTheArgument()
    {
        // Arrange
        var receivedArguments = new List<int>();
        var externalEvent = new ExternalEvent<int>(receivedArguments.Add, ExternalEventOptions.AllowDirectInvocation);

        // Act
        externalEvent.Raise(1);
        externalEvent.Raise(2);

        // Assert
        await Assert.That(receivedArguments).IsEquivalentTo([1, 2]);
    }

    [Test]
    public async Task GetName_DefaultIdentifier_ReturnsTheTypeName()
    {
        // Arrange
        var externalEvent = new ExternalEvent(() => { });

        // Act
        var name = externalEvent.GetName();

        // Assert
        await Assert.That(name).IsEqualTo(nameof(ExternalEvent));
    }

    [Test]
    public async Task Constructor_NullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.That(() => new ExternalEvent((Action)null!)).Throws<ArgumentNullException>();
    }
}
