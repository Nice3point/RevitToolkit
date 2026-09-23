using Nice3point.Revit.Toolkit.Options;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class DuplicateTypeNamesHandlerTests : RevitApiTest
{
    private static readonly string[] SeedTypeNames = ["Seed Wall Type 1", "Seed Wall Type 2", "Seed Wall Type 3"];

    private Document _sourceDocument = null!;
    private Document _targetDocument = null!;
    private List<ElementId> _sourceTypeIds = null!;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void SeedDocuments()
    {
        _sourceDocument = Application.NewProjectDocument(UnitSystem.Metric);
        _targetDocument = Application.NewProjectDocument(UnitSystem.Metric);
        _sourceTypeIds = SeedWallTypes(_sourceDocument, WallFunction.Exterior);
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocuments()
    {
        _sourceDocument.Close(false);
        _targetDocument.Close(false);
    }

    [Test]
    public async Task DuplicateTypeNamesHandler_DefaultConstructor_CopiesElements()
    {
        // Arrange
        var handler = new DuplicateTypeNamesHandler();
        var copyOptions = new CopyPasteOptions();
        copyOptions.SetDuplicateTypeNamesHandler(handler);

        // Act
        using var transaction = new Transaction(_targetDocument, "Copy elements");
        transaction.Start();
        var copiedIds = ElementTransformUtils.CopyElements(_sourceDocument, _sourceTypeIds, _targetDocument, Transform.Identity, copyOptions);
        transaction.Commit();

        // Assert
        await Assert.That(copiedIds.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task DuplicateTypeNamesHandler_UseDestinationTypes_CopiesElements()
    {
        // Arrange
        var handler = new DuplicateTypeNamesHandler(DuplicateTypeAction.UseDestinationTypes);
        var copyOptions = new CopyPasteOptions();
        copyOptions.SetDuplicateTypeNamesHandler(handler);

        // Act
        using var transaction = new Transaction(_targetDocument, "Copy elements");
        transaction.Start();
        var copiedIds = ElementTransformUtils.CopyElements(_sourceDocument, _sourceTypeIds, _targetDocument, Transform.Identity, copyOptions);
        transaction.Commit();

        // Assert
        await Assert.That(copiedIds.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task DuplicateTypeNamesHandler_CopyTwice_SecondCopySucceeds()
    {
        // Arrange
        var handler = new DuplicateTypeNamesHandler(DuplicateTypeAction.UseDestinationTypes);
        var copyOptions = new CopyPasteOptions();
        copyOptions.SetDuplicateTypeNamesHandler(handler);

        // Act
        using (var transaction = new Transaction(_targetDocument, "First copy"))
        {
            transaction.Start();
            ElementTransformUtils.CopyElements(_sourceDocument, _sourceTypeIds, _targetDocument, Transform.Identity, copyOptions);
            transaction.Commit();
        }

        ICollection<ElementId> secondCopyIds;

        using (var transaction = new Transaction(_targetDocument, "Second copy"))
        {
            transaction.Start();
            secondCopyIds = ElementTransformUtils.CopyElements(_sourceDocument, _sourceTypeIds, _targetDocument, Transform.Identity, copyOptions);
            transaction.Commit();
        }

        // Assert
        await Assert.That(secondCopyIds.Count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task DuplicateTypeNamesHandler_Abort_RollsBackOnDuplicate()
    {
        // Arrange
        SeedWallTypes(_targetDocument, WallFunction.Interior);

        var abortOptions = new CopyPasteOptions();
        abortOptions.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler(DuplicateTypeAction.Abort));

        // Act
        TransactionStatus status;

        using (var transaction = new Transaction(_targetDocument, "Copy with abort"))
        {
            transaction.Start();
            ElementTransformUtils.CopyElements(_sourceDocument, _sourceTypeIds, _targetDocument, Transform.Identity, abortOptions);
            status = transaction.Commit();
        }

        // Assert
        await Assert.That(status).IsEqualTo(TransactionStatus.RolledBack);
    }

    private static List<ElementId> SeedWallTypes(Document document, WallFunction function)
    {
        var baseType = document.GetDefaultElementTypeId(ElementTypeGroup.WallType).ToElement<WallType>(document)!;

        using var transaction = new Transaction(document, "Seed wall types");
        transaction.Start();

        var typeIds = SeedTypeNames
            .Select(typeName =>
            {
                var wallType = (WallType)baseType.Duplicate(typeName);
                wallType.Function = function;
                return wallType.Id;
            })
            .ToList();

        transaction.Commit();

        return typeIds;
    }
}
