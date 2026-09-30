using Nice3point.Revit.Toolkit.Options;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class SelectionConfigurationTests : RevitApiUiTest
{
    private static Document _document = null!;
    private static Level _level = null!;
    private static View _view = null!;

    [Before(Class)]
    [HookExecutor<RevitUiThreadExecutor>]
    public static void CreateDocument()
    {
        _document = UiApplication.Application.NewProjectDocument(UnitSystem.Metric);
        _level = _document.CollectElements()
            .Instances()
            .OfClass<Level>()
            .Cast<Level>()
            .First();

        _view = _document.CollectElements()
            .Instances()
            .OfClass<View>()
            .Cast<View>()
            .First(view => !view.IsTemplate);
    }

    [After(Class)]
    [HookExecutor<RevitUiThreadExecutor>]
    public static void CloseDocument()
    {
        _document.Close(false);
    }

    [Test]
    public async Task Filter_WithoutHandlers_AllowsElementAndReference()
    {
        // Arrange
        var configuration = new SelectionConfiguration();

        // Act
        var isElementAllowed = configuration.Filter.AllowElement(_level);
        var isReferenceAllowed = configuration.Filter.AllowReference(new Reference(_level), XYZ.Zero);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isElementAllowed).IsTrue();
            await Assert.That(isReferenceAllowed).IsTrue();
        }
    }

    [Test]
    public async Task AllowElement_LevelHandler_AllowsOnlyLevels()
    {
        // Arrange
        var configuration = new SelectionConfiguration()
            .Allow.Element(element => element is Level);

        // Act
        var isLevelAllowed = configuration.Filter.AllowElement(_level);
        var isViewAllowed = configuration.Filter.AllowElement(_view);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isLevelAllowed).IsTrue();
            await Assert.That(isViewAllowed).IsFalse();
        }
    }

    [Test]
    public async Task AllowElement_ElementHandler_KeepsReferencesAllowed()
    {
        // Arrange
        var configuration = new SelectionConfiguration()
            .Allow.Element(_ => false);

        // Act
        var isReferenceAllowed = configuration.Filter.AllowReference(new Reference(_level), XYZ.Zero);

        // Assert
        await Assert.That(isReferenceAllowed).IsTrue();
    }

    [Test]
    public async Task AllowReference_ReferenceHandler_ReceivesReferenceAndPosition()
    {
        // Arrange
        Reference? receivedReference = null;
        XYZ? receivedPosition = null;
        var reference = new Reference(_level);
        var position = new XYZ(1, 2, 3);

        var configuration = new SelectionConfiguration()
            .Allow.Reference((candidate, candidatePosition) =>
            {
                receivedReference = candidate;
                receivedPosition = candidatePosition;
                return false;
            });

        // Act
        var isReferenceAllowed = configuration.Filter.AllowReference(reference, position);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isReferenceAllowed).IsFalse();
            await Assert.That(receivedReference!.ElementId).IsEqualTo(_level.Id);
            await Assert.That(receivedPosition!.IsAlmostEqualTo(position)).IsTrue();
        }
    }

    [Test]
    public async Task AllowReference_ReferenceHandler_KeepsElementsAllowed()
    {
        // Arrange
        var configuration = new SelectionConfiguration()
            .Allow.Reference((_, _) => false);

        // Act
        var isElementAllowed = configuration.Filter.AllowElement(_level);

        // Assert
        await Assert.That(isElementAllowed).IsTrue();
    }

    [Test]
    public async Task AllowElement_HandlerReassigned_UsesTheLastHandler()
    {
        // Arrange
        var configuration = new SelectionConfiguration()
            .Allow.Element(_ => false)
            .Allow.Element(_ => true);

        // Act
        var isElementAllowed = configuration.Filter.AllowElement(_level);

        // Assert
        await Assert.That(isElementAllowed).IsTrue();
    }

    [Test]
    public async Task Allow_ChainedHandlers_ReturnsTheSameConfiguration()
    {
        // Arrange
        var configuration = new SelectionConfiguration();

        // Act
        var elementConfiguration = configuration.Allow.Element(_ => true);
        var referenceConfiguration = elementConfiguration.Allow.Reference((_, _) => true);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(elementConfiguration).IsSameReferenceAs(configuration);
            await Assert.That(referenceConfiguration).IsSameReferenceAs(configuration);
        }
    }
}
