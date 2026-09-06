using Nice3point.Revit.Toolkit.Options;
using Nice3point.Revit.Toolkit.Tests.Abstractions;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class FamilyLoadOptionsTests : RevitFamilySampleTest
{
    private Document _document = null!;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument()
    {
        _document.Close(false);
    }

    [Test]
    [MethodDataSource(nameof(RevitFamilies))]
    public async Task FamilyLoadOptions_DefaultConstructor_LoadsFamily(string path)
    {
        // Arrange
        var options = new FamilyLoadOptions();

        // Act
        using var transaction = new Transaction(_document, "Load Family");
        transaction.Start();
        var result = _document.LoadFamily(path, options, out var family);
        transaction.Commit();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            await Assert.That(family).IsNotNull();
            await Assert.That(family.IsValidObject).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(RevitFamilies))]
    public async Task FamilyLoadOptions_WithOverwriteTrue_ReloadsFamily(string path)
    {
        // Arrange
        var options = new FamilyLoadOptions(true);

        // Act
        using (var transaction = new Transaction(_document, "First Load"))
        {
            transaction.Start();
            _document.LoadFamily(path, options, out _);
            transaction.Commit();
        }

        bool reloadResult;

        using (var transaction = new Transaction(_document, "Reload Family"))
        {
            transaction.Start();
            reloadResult = _document.LoadFamily(path, options, out _);
            transaction.Commit();
        }

        // Assert
        await Assert.That(reloadResult).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(RevitFamilies))]
    public async Task FamilyLoadOptions_WithOverwriteFalse_LoadsFamily(string path)
    {
        // Arrange
        var options = new FamilyLoadOptions(false);

        // Act
        using var transaction = new Transaction(_document, "Load Family");
        transaction.Start();
        var result = _document.LoadFamily(path, options, out var family);
        transaction.Commit();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            await Assert.That(family).IsNotNull();
        }
    }

    [Test]
    [MethodDataSource(nameof(RevitFamilies))]
    public async Task FamilyLoadOptions_WithFamilySourceProject_LoadsFamily(string path)
    {
        // Arrange
        var options = new FamilyLoadOptions(true, FamilySource.Project);

        // Act
        using var transaction = new Transaction(_document, "Load Family");
        transaction.Start();
        var result = _document.LoadFamily(path, options, out var family);
        transaction.Commit();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            await Assert.That(family).IsNotNull();
        }
    }

    [Test]
    [MethodDataSource(nameof(RevitFamilies))]
    public async Task FamilyLoadOptions_WithFamilySourceFamily_LoadsFamily(string path)
    {
        // Arrange
        var options = new FamilyLoadOptions(true, FamilySource.Family);

        // Act
        using var transaction = new Transaction(_document, "Load Family");
        transaction.Start();
        var result = _document.LoadFamily(path, options, out var family);
        transaction.Commit();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            await Assert.That(family).IsNotNull();
        }
    }

    [Test]
    [MethodDataSource(nameof(RevitFamilies))]
    public async Task FamilyLoadOptions_LoadedFamilyHasSymbols(string path)
    {
        // Arrange
        var options = new FamilyLoadOptions();

        // Act
        using var transaction = new Transaction(_document, "Load Family");
        transaction.Start();
        _document.LoadFamily(path, options, out var family);
        transaction.Commit();

        var symbolIds = family.GetFamilySymbolIds();

        // Assert
        await Assert.That(symbolIds.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(RevitFamilies))]
    public async Task FamilyLoadOptions_TransactionCommits_Successfully(string path)
    {
        // Arrange
        var options = new FamilyLoadOptions();

        // Act
        using var transaction = new Transaction(_document, "Load Family");
        transaction.Start();
        _document.LoadFamily(path, options, out _);
        var status = transaction.Commit();

        // Assert
        await Assert.That(status).IsEqualTo(TransactionStatus.Committed);
    }

    [Test]
    public async Task FamilyLoadOptions_MultipleFamilies_AllLoad()
    {
        // Arrange
        if (RevitFamilies.Length == 0)
        {
            Skip.Test("No sample files available");
            return;
        }

        var options = new FamilyLoadOptions();
        var loadedCount = 0;

        // Act
        using var transaction = new Transaction(_document, "Load Multiple Families");
        transaction.Start();

        foreach (var familyPath in RevitFamilies)
        {
            if (_document.LoadFamily(familyPath, options, out _))
            {
                loadedCount++;
            }
        }

        transaction.Commit();

        // Assert
        await Assert.That(loadedCount).IsEqualTo(RevitFamilies.Length);
    }
}
