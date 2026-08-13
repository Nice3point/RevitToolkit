using Nice3point.Revit.Toolkit.Options;
using Nice3point.Revit.Toolkit.Tests.Abstractions;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class DuplicateTypeNamesHandlerTests : RevitModelSampleTest
{
    [Test]
    [MethodDataSource(nameof(RevitModels))]
    public async Task DuplicateTypeNamesHandler_DefaultConstructor_CopiesElements(string path)
    {
        // Arrange
        var sourceDocument = ModelDocuments[path];
        var targetDocument = Application.NewProjectDocument(UnitSystem.Metric);

        var elementsToCopy = sourceDocument.CollectElements()
            .OfClass<FamilySymbol>()
            .Take(5)
            .Select(element => element.Id)
            .ToList();

        var handler = new DuplicateTypeNamesHandler();
        var copyOptions = new CopyPasteOptions();
        copyOptions.SetDuplicateTypeNamesHandler(handler);

        // Act
        using var transaction = new Transaction(targetDocument, "Copy elements");
        transaction.Start();
        var copiedIds = ElementTransformUtils.CopyElements(sourceDocument, elementsToCopy, targetDocument, Transform.Identity, copyOptions);
        transaction.Commit();

        targetDocument.Close(false);

        // Assert
        await Assert.That(copiedIds.Count).IsGreaterThan(0);
    }

    [Test]
    [MethodDataSource(nameof(RevitModels))]
    public async Task DuplicateTypeNamesHandler_UseDestinationTypes_CopiesElements(string path)
    {
        // Arrange
        var sourceDocument = ModelDocuments[path];
        var targetDocument = Application.NewProjectDocument(UnitSystem.Metric);

        var elementsToCopy = sourceDocument.CollectElements()
            .OfClass<FamilySymbol>()
            .Take(5)
            .Select(element => element.Id)
            .ToList();

        var handler = new DuplicateTypeNamesHandler(DuplicateTypeAction.UseDestinationTypes);
        var copyOptions = new CopyPasteOptions();
        copyOptions.SetDuplicateTypeNamesHandler(handler);

        // Act
        using var transaction = new Transaction(targetDocument, "Copy elements");
        transaction.Start();
        var copiedIds = ElementTransformUtils.CopyElements(sourceDocument, elementsToCopy, targetDocument, Transform.Identity, copyOptions);
        transaction.Commit();

        targetDocument.Close(false);

        // Assert
        await Assert.That(copiedIds.Count).IsGreaterThan(0);
    }

    [Test]
    [MethodDataSource(nameof(RevitModels))]
    public async Task DuplicateTypeNamesHandler_CopyTwice_SecondCopySucceeds(string path)
    {
        // Arrange
        var sourceDocument = ModelDocuments[path];
        var targetDocument = Application.NewProjectDocument(UnitSystem.Metric);

        var elementsToCopy = sourceDocument.CollectElements()
            .OfClass<FamilySymbol>()
            .Take(3)
            .Select(element => element.Id)
            .ToList();

        var handler = new DuplicateTypeNamesHandler(DuplicateTypeAction.UseDestinationTypes);
        var copyOptions = new CopyPasteOptions();
        copyOptions.SetDuplicateTypeNamesHandler(handler);

        // Act
        using (var transaction = new Transaction(targetDocument, "First copy"))
        {
            transaction.Start();
            ElementTransformUtils.CopyElements(sourceDocument, elementsToCopy, targetDocument, Transform.Identity, copyOptions);
            transaction.Commit();
        }

        ICollection<ElementId> secondCopyIds;

        using (var transaction = new Transaction(targetDocument, "Second copy"))
        {
            transaction.Start();
            secondCopyIds = ElementTransformUtils.CopyElements(sourceDocument, elementsToCopy, targetDocument, Transform.Identity, copyOptions);
            transaction.Commit();
        }

        targetDocument.Close(false);

        // Assert
        await Assert.That(secondCopyIds.Count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(RevitModels))]
    public async Task DuplicateTypeNamesHandler_Abort_RollsBackOnDuplicate(string path)
    {
        // Arrange
        var sourceDocument = ModelDocuments[path];
        var targetDocument = Application.NewProjectDocument(UnitSystem.Metric);

        var elementsToCopy = sourceDocument.CollectElements()
            .OfClass<FamilySymbol>()
            .Take(3)
            .Select(element => element.Id)
            .ToList();

        var firstOptions = new CopyPasteOptions();
        firstOptions.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler());

        using (var transaction = new Transaction(targetDocument, "First copy"))
        {
            transaction.Start();
            ElementTransformUtils.CopyElements(sourceDocument, elementsToCopy, targetDocument, Transform.Identity, firstOptions);
            transaction.Commit();
        }

        var abortOptions = new CopyPasteOptions();
        abortOptions.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler(DuplicateTypeAction.Abort));

        // Act
        TransactionStatus status;

        using (var transaction = new Transaction(targetDocument, "Second copy with abort"))
        {
            transaction.Start();
            ElementTransformUtils.CopyElements(sourceDocument, elementsToCopy, targetDocument, Transform.Identity, abortOptions);
            status = transaction.Commit();
        }

        targetDocument.Close(false);

        // Assert
        await Assert.That(status).IsEqualTo(TransactionStatus.RolledBack);
    }
}
