using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.Decorators;
using Nice3point.TUnit.Revit;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class DockablePaneProviderTests : RevitApiUiTest
{
    [Test]
    public async Task Register_FluentConfiguration_RegistersThePane()
    {
        // Arrange
        var paneId = new DockablePaneId(Guid.NewGuid());
        var controlledApplication = UiApplication.AsControlledApplication();

        // Act
        DockablePaneProvider.Register(controlledApplication)
            .SetId(paneId)
            .SetTitle("Fluent pane")
            .SetConfiguration(data => data.VisibleByDefault = false);

        // Assert
        await Assert.That(DockablePane.PaneIsRegistered(paneId)).IsTrue();
    }

    [Test]
    public async Task Register_GuidAndTitle_RegistersThePane()
    {
        // Arrange
        var paneGuid = Guid.NewGuid();
        var controlledApplication = UiApplication.AsControlledApplication();

        // Act
        DockablePaneProvider.Register(controlledApplication, paneGuid, "Guid pane")
            .SetConfiguration(data => data.VisibleByDefault = false);

        // Assert
        await Assert.That(DockablePane.PaneIsRegistered(new DockablePaneId(paneGuid))).IsTrue();
    }

    [Test]
    public async Task Register_PaneIdAndTitle_RegistersThePane()
    {
        // Arrange
        var paneId = new DockablePaneId(Guid.NewGuid());
        var controlledApplication = UiApplication.AsControlledApplication();

        // Act
        DockablePaneProvider.Register(controlledApplication, paneId, "Id pane")
            .SetConfiguration(data => data.VisibleByDefault = false);

        // Assert
        await Assert.That(DockablePane.PaneIsRegistered(paneId)).IsTrue();
    }
}
