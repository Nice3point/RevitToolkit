using System.IO;
using Autodesk.Revit.UI;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class RevitContextTests : RevitApiUiTest
{
    private static UIDocument _uiDocument = null!;

    [Before(Class)]
    [HookExecutor<RevitUiThreadExecutor>]
    public static void OpenModel()
    {
        var modelPath = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.rvt");
        var document = UiApplication.Application.NewProjectDocument(UnitSystem.Metric);
        document.SaveAs(modelPath);
        document.Close(false);

        _uiDocument = UiApplication.OpenAndActivateDocument(modelPath);
    }

    [Test]
    public async Task UiApplication_WhenAccessed_ReturnsTheSessionApplication()
    {
        // Act
        var uiApplication = RevitContext.UiApplication;

        // Assert
        await Assert.That(uiApplication.MainWindowHandle).IsEqualTo(UiApplication.MainWindowHandle);
    }

    [Test]
    public async Task IsRevitInApiMode_InsideTheApiContext_ReturnsTrue()
    {
        // Act
        var isRevitInApiMode = RevitContext.IsRevitInApiMode;

        // Assert
        await Assert.That(isRevitInApiMode).IsTrue();
    }

    [Test]
    public async Task ActiveDocument_OpenedModel_ReturnsTheActiveDocument()
    {
        // Act
        var activeUiDocument = RevitContext.ActiveUiDocument;
        var activeDocument = RevitContext.ActiveDocument;

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(activeUiDocument!.Document.PathName).IsEqualTo(_uiDocument.Document.PathName);
            await Assert.That(activeDocument!.PathName).IsEqualTo(_uiDocument.Document.PathName);
        }
    }

    [Test]
    public async Task ActiveView_OpenedModel_ReturnsTheActiveView()
    {
        // Act
        var activeView = RevitContext.ActiveView;
        var activeGraphicalView = RevitContext.ActiveGraphicalView;

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(activeView!.Id).IsEqualTo(_uiDocument.ActiveView.Id);
            await Assert.That(activeGraphicalView!.Id).IsEqualTo(_uiDocument.ActiveGraphicalView.Id);
        }
    }

    [Test]
    public async Task ActiveView_SetAnotherView_ActivatesTheView()
    {
        // Arrange
        var initialViewId = _uiDocument.ActiveView.Id;
        var targetView = _uiDocument.Document.CollectElements()
            .Instances()
            .OfClass<ViewPlan>()
            .Cast<ViewPlan>()
            .FirstOrDefault(view => !view.IsTemplate && view.Id != initialViewId);

        if (targetView is null)
        {
            Skip.Test("The model has no second floor plan view");
            return;
        }

        // Act
        RevitContext.ActiveView = targetView;

        // Assert
        await Assert.That(_uiDocument.ActiveView.Id).IsEqualTo(targetView.Id);
    }

    [Test]
    public async Task BeginDialogSuppressionScope_TaskDialogResult_ReturnsTheResultWithoutShowing()
    {
        // Arrange
        using var scope = RevitContext.BeginDialogSuppressionScope(TaskDialogResult.Yes);

        // Act
        var result = TaskDialog.Show("Suppressed dialog", "Suppressed dialog", TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

        // Assert
        await Assert.That(result).IsEqualTo(TaskDialogResult.Yes);
    }

    [Test]
    public async Task BeginDialogSuppressionScope_ResultCode_ReturnsTheResultWithoutShowing()
    {
        // Arrange
        using var scope = RevitContext.BeginDialogSuppressionScope((int)TaskDialogResult.No);

        // Act
        var result = TaskDialog.Show("Suppressed dialog", "Suppressed dialog", TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

        // Assert
        await Assert.That(result).IsEqualTo(TaskDialogResult.No);
    }

    [Test]
    public async Task BeginDialogSuppressionScope_Handler_ReceivesTheDialog()
    {
        // Arrange
        var dialogIds = new List<string>();
        using var scope = RevitContext.BeginDialogSuppressionScope(args =>
        {
            dialogIds.Add(args.DialogId);
            args.OverrideResult((int)TaskDialogResult.Ok);
        });

        var dialog = new TaskDialog("Suppressed dialog")
        {
            Id = "Nice3point.Revit.Toolkit.Tests.SuppressedDialog",
            MainContent = "Suppressed dialog",
            CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel
        };

        // Act
        var result = dialog.Show();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsEqualTo(TaskDialogResult.Ok);
            await Assert.That(dialogIds).IsEquivalentTo([dialog.Id]);
        }
    }

    [Test]
    public async Task BeginDialogSuppressionScope_InnerScopeDisposed_KeepsSuppressingDialogs()
    {
        // Arrange
        using var outerScope = RevitContext.BeginDialogSuppressionScope(TaskDialogResult.Yes);
        RevitContext.BeginDialogSuppressionScope(TaskDialogResult.Yes).Dispose();

        // Act
        var result = TaskDialog.Show("Suppressed dialog", "Suppressed dialog", TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

        // Assert
        await Assert.That(result).IsEqualTo(TaskDialogResult.Yes);
    }

    [Test]
    public async Task BeginDialogSuppressionScope_DisposedTwice_DoesNotThrow()
    {
        // Arrange
        var scope = RevitContext.BeginDialogSuppressionScope();

        // Act & Assert
        await Assert.That(() =>
        {
            scope.Dispose();
            scope.Dispose();
        }).ThrowsNothing();
    }
}
