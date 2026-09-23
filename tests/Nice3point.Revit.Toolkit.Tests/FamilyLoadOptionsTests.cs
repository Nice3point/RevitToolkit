using Nice3point.Revit.Toolkit.Options;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class FamilyLoadOptionsTests : RevitApiTest
{
    private const string FamilyTemplateName = "Metric Generic Model.rft";

    private readonly string _seedDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private string _familyTemplatePath = null!;
    private string _familyPath = null!;
    private Document? _document;

    private Document Document => _document!;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void SeedDocuments()
    {
        var familyTemplatePath = FindFamilyTemplate();
        if (familyTemplatePath is null)
        {
            Skip.Test($"The '{FamilyTemplateName}' family template is not installed");
            return;
        }

        _familyTemplatePath = familyTemplatePath;
        Directory.CreateDirectory(_seedDirectory);

        _document = Application.NewProjectDocument(UnitSystem.Metric);
        _familyPath = SeedFamily("Seed Family");
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocuments()
    {
        _document?.Close(false);

        if (Directory.Exists(_seedDirectory))
        {
            Directory.Delete(_seedDirectory, true);
        }
    }

    [Test]
    public async Task FamilyLoadOptions_DefaultConstructor_LoadsFamily()
    {
        // Arrange
        var options = new FamilyLoadOptions();

        // Act
        using var transaction = new Transaction(Document, "Load Family");
        transaction.Start();
        var result = Document.LoadFamily(_familyPath, options, out var family);
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
    public async Task FamilyLoadOptions_WithOverwriteTrue_ReloadsFamily()
    {
        // Arrange
        var options = new FamilyLoadOptions(true);

        // Act
        using (var transaction = new Transaction(Document, "First Load"))
        {
            transaction.Start();
            Document.LoadFamily(_familyPath, options, out _);
            transaction.Commit();
        }

        bool reloadResult;

        using (var transaction = new Transaction(Document, "Reload Family"))
        {
            transaction.Start();
            reloadResult = Document.LoadFamily(_familyPath, options, out _);
            transaction.Commit();
        }

        // Assert
        await Assert.That(reloadResult).IsFalse();
    }

    [Test]
    public async Task FamilyLoadOptions_WithOverwriteFalse_LoadsFamily()
    {
        // Arrange
        var options = new FamilyLoadOptions(false);

        // Act
        using var transaction = new Transaction(Document, "Load Family");
        transaction.Start();
        var result = Document.LoadFamily(_familyPath, options, out var family);
        transaction.Commit();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            await Assert.That(family).IsNotNull();
        }
    }

    [Test]
    public async Task FamilyLoadOptions_WithFamilySourceProject_LoadsFamily()
    {
        // Arrange
        var options = new FamilyLoadOptions(true, FamilySource.Project);

        // Act
        using var transaction = new Transaction(Document, "Load Family");
        transaction.Start();
        var result = Document.LoadFamily(_familyPath, options, out var family);
        transaction.Commit();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            await Assert.That(family).IsNotNull();
        }
    }

    [Test]
    public async Task FamilyLoadOptions_WithFamilySourceFamily_LoadsFamily()
    {
        // Arrange
        var options = new FamilyLoadOptions(true, FamilySource.Family);

        // Act
        using var transaction = new Transaction(Document, "Load Family");
        transaction.Start();
        var result = Document.LoadFamily(_familyPath, options, out var family);
        transaction.Commit();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            await Assert.That(family).IsNotNull();
        }
    }

    [Test]
    public async Task FamilyLoadOptions_LoadedFamilyHasSymbols()
    {
        // Arrange
        var options = new FamilyLoadOptions();

        // Act
        using var transaction = new Transaction(Document, "Load Family");
        transaction.Start();
        Document.LoadFamily(_familyPath, options, out var family);
        transaction.Commit();

        var symbolIds = family.GetFamilySymbolIds();

        // Assert
        await Assert.That(symbolIds.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task FamilyLoadOptions_TransactionCommits_Successfully()
    {
        // Arrange
        var options = new FamilyLoadOptions();

        // Act
        using var transaction = new Transaction(Document, "Load Family");
        transaction.Start();
        Document.LoadFamily(_familyPath, options, out _);
        var status = transaction.Commit();

        // Assert
        await Assert.That(status).IsEqualTo(TransactionStatus.Committed);
    }

    [Test]
    public async Task FamilyLoadOptions_MultipleFamilies_AllLoad()
    {
        // Arrange
        string[] familyPaths = [_familyPath, SeedFamily("Second Seed Family"), SeedFamily("Third Seed Family")];

        var options = new FamilyLoadOptions();
        var loadedCount = 0;

        // Act
        using var transaction = new Transaction(Document, "Load Multiple Families");
        transaction.Start();

        foreach (var familyPath in familyPaths)
        {
            if (Document.LoadFamily(familyPath, options, out _))
            {
                loadedCount++;
            }
        }

        transaction.Commit();

        // Assert
        await Assert.That(loadedCount).IsEqualTo(familyPaths.Length);
    }

    private string? FindFamilyTemplate()
    {
        var templatesDirectory = Application.FamilyTemplatePath;
        if (!Directory.Exists(templatesDirectory))
        {
            return null;
        }

        return Directory.EnumerateFiles(templatesDirectory, FamilyTemplateName, SearchOption.AllDirectories).FirstOrDefault();
    }

    private string SeedFamily(string familyName)
    {
        var familyDocument = Application.NewFamilyDocument(_familyTemplatePath);

        using (var transaction = new Transaction(familyDocument, "Seed family"))
        {
            transaction.Start();
            familyDocument.FamilyManager.NewType($"{familyName} Type");
            transaction.Commit();
        }

        var familyPath = Path.Combine(_seedDirectory, $"{familyName}.rfa");
        familyDocument.SaveAs(familyPath);
        familyDocument.Close(false);

        return familyPath;
    }
}
