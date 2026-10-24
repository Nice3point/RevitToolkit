using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;
using TUnit.Assertions.Enums;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class ExternalEventCoreTests : RevitApiUiTest
{
    [Test]
    [Arguments(nameof(ExternalEventRequest.Denied))]
    [Arguments(nameof(ExternalEventRequest.TimedOut))]
    public async Task Raise_RefusedRequest_NeverExecutesTheArgument(string refusalName)
    {
        // Arrange
        var refusal = (ExternalEventRequest)Enum.Parse(typeof(ExternalEventRequest), refusalName);
        var raiseResults = new Queue<ExternalEventRequest>([refusal, ExternalEventRequest.Accepted]);
        var receivedArguments = new List<int>();
        var core = new ExternalEventCore<int>((_, argument) => receivedArguments.Add(argument), raiseResults.Dequeue);

        // Act
        var refusedRequest = core.Raise(1);
        var acceptedRequest = core.Raise(2);
        core.Execute(null!);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(refusedRequest).IsEqualTo(refusal);
            await Assert.That(acceptedRequest).IsEqualTo(ExternalEventRequest.Accepted);
            await Assert.That(receivedArguments).IsEquivalentTo([2]);
        }
    }

    [Test]
    public async Task Execute_RefusedRaiseForTheRest_ExecutesTheRestInTheSameExecution()
    {
        // Arrange
        var raiseResults = new Queue<ExternalEventRequest>([ExternalEventRequest.Accepted, ExternalEventRequest.Pending, ExternalEventRequest.TimedOut]);
        var receivedArguments = new List<int>();
        var core = new ExternalEventCore<int>((_, argument) => receivedArguments.Add(argument), raiseResults.Dequeue);
        core.Raise(1);
        core.Raise(2);

        // Act
        core.Execute(null!);

        // Assert
        await Assert.That(receivedArguments).IsEquivalentTo([1, 2], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Execute_AcceptedRaiseForTheRest_ExecutesTheOldestRaise()
    {
        // Arrange
        var receivedArguments = new List<int>();
        var core = new ExternalEventCore<int>((_, argument) => receivedArguments.Add(argument), () => ExternalEventRequest.Accepted);
        core.Raise(1);
        core.Raise(2);

        // Act
        core.Execute(null!);

        // Assert
        await Assert.That(receivedArguments).IsEquivalentTo([1]);
    }
}
