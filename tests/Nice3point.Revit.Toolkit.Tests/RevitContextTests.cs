using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
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
    public async Task IsRevitInApiMode_OutsideTheRevitThread_ReturnsFalse()
    {
        // Act
        var isRevitInApiMode = await Task.Run(() => RevitContext.IsRevitInApiMode);

        // Assert
        await Assert.That(isRevitInApiMode).IsFalse();
    }

    [Test]
    [Timeout(10_000)]
    public async Task IsRevitInApiMode_RevitThreadOutsideTheApiContext_ReturnsFalse(CancellationToken cancellationToken)
    {
        // Arrange
        var revitThreadId = Environment.CurrentManagedThreadId;
        var completion = new TaskCompletionSource<(int ThreadId, bool IsRevitInApiMode)>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        Dispatcher.CurrentDispatcher.BeginInvoke(() => completion.SetResult((Environment.CurrentManagedThreadId, RevitContext.IsRevitInApiMode)));
        var (threadId, isRevitInApiMode) = await completion.Task.WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(threadId).IsEqualTo(revitThreadId);
            await Assert.That(isRevitInApiMode).IsFalse();
        }
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

    [Test]
    public async Task BeginModalLoopCompatibilityScope_TemplateOfThePreviousVersion_WindowDrawsTheNextFrame(CancellationToken cancellationToken)
    {
        // Arrange
        var previousVersion = int.Parse(UiApplication.Application.VersionNumber, CultureInfo.InvariantCulture) - 1;
        var templatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", $"RVT {previousVersion}", "Family Templates", "English", "Metric Detail Item.rft");
        if (!File.Exists(templatePath))
        {
            Skip.Test($"Revit {previousVersion} with the English content is not installed");
            return;
        }

        using var window = await ColorWindow.ShowAsync(Colors.Red, cancellationToken);

        // Act
        using (RevitContext.BeginModalLoopCompatibilityScope())
        {
            UiApplication.Application.NewFamilyDocument(templatePath).Close(false);
        }

        window.Fill(Colors.Blue);

        // Assert
        await Assert.That(await window.WaitForDrawnColorAsync(Colors.Blue, cancellationToken)).IsEqualTo(Colors.Blue);
    }

    [Test]
    public async Task BeginModalLoopCompatibilityScope_DisposedTwice_DoesNotThrow()
    {
        // Arrange
        var scope = RevitContext.BeginModalLoopCompatibilityScope();

        // Act & Assert
        await Assert.That(() =>
        {
            scope.Dispose();
            scope.Dispose();
        }).ThrowsNothing();
    }

    [Test]
    public async Task BeginModalLoopCompatibilityScope_DisposedOnThreadWithoutWpf_DoesNotThrow()
    {
        // Act & Assert
        await Assert.That(() => Task.Run(() => RevitContext.BeginModalLoopCompatibilityScope().Dispose())).ThrowsNothing();
    }
}
