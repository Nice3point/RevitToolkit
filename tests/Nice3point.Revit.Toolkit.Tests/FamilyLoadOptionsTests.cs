using Nice3point.Revit.Toolkit.Options;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class FamilyLoadOptionsTests : RevitApiTest
{
    private static readonly string SamplesPath = $@"C:\Program Files\Autodesk\Revit {Application.VersionNumber}\Samples";

    [Before(Class)]
    public static void ValidateSamples()
    {
        if (!Directory.Exists(SamplesPath))
        {
            Skip.Test($"Samples folder not found at {SamplesPath}");
            return;
        }

        if (!Directory.EnumerateFiles(SamplesPath, "*.rfa").Any())
        {
            Skip.Test($"No .rfa files found in {SamplesPath}");
        }
    }

    public static IEnumerable<string> GetSampleRfaFiles()
    {
        if (!Directory.Exists(SamplesPath))
        {
            yield return string.Empty;
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(SamplesPath, "*.rfa"))
        {
            yield return file;
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRfaFiles))]
    public async Task FamilyLoadOptions_DefaultConstructor_LoadsFamily(string familyPath)
    {
        // Arrange
        var document = Application.NewProjectDocument(UnitSystem.Metric);

        try
        {
            var options = new FamilyLoadOptions();

            // Act
            using var transaction = new Transaction(document, "Load Family");
            transaction.Start();

            var result = document.LoadFamily(familyPath, options, out var family);

            transaction.Commit();

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(result).IsTrue();
                await Assert.That(family).IsNotNull();
                await Assert.That(family.IsValidObject).IsTrue();
            }
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRfaFiles))]
    public async Task FamilyLoadOptions_WithOverwriteTrue_ReloadsFamily(string familyPath)
    {
        // Arrange
        var document = Application.NewProjectDocument(UnitSystem.Metric);

        try
        {
            var options = new FamilyLoadOptions(overwrite: true);

            // Act - first load
            using (var transaction = new Transaction(document, "First Load"))
            {
                transaction.Start();
                var loadResult = document.LoadFamily(familyPath, options, out _);
                transaction.Commit();

                await Assert.That(loadResult).IsTrue();
            }

            // Act - reload
            using (var transaction = new Transaction(document, "Reload Family"))
            {
                transaction.Start();
                var reloadResult = document.LoadFamily(familyPath, options, out _);
                transaction.Commit();

                // Assert
                await Assert.That(reloadResult).IsFalse();
            }
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRfaFiles))]
    public async Task FamilyLoadOptions_WithOverwriteFalse_LoadsFamily(string familyPath)
    {
        // Arrange
        var document = Application.NewProjectDocument(UnitSystem.Metric);

        try
        {
            var options = new FamilyLoadOptions(overwrite: false);

            // Act
            using var transaction = new Transaction(document, "Load Family");
            transaction.Start();

            var result = document.LoadFamily(familyPath, options, out var family);

            transaction.Commit();

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(result).IsTrue();
                await Assert.That(family).IsNotNull();
            }
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRfaFiles))]
    public async Task FamilyLoadOptions_WithFamilySourceProject_LoadsFamily(string familyPath)
    {
        // Arrange
        var document = Application.NewProjectDocument(UnitSystem.Metric);

        try
        {
            var options = new FamilyLoadOptions(overwrite: true, familySource: FamilySource.Project);

            // Act
            using var transaction = new Transaction(document, "Load Family");
            transaction.Start();

            var result = document.LoadFamily(familyPath, options, out var family);

            transaction.Commit();

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(result).IsTrue();
                await Assert.That(family).IsNotNull();
            }
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRfaFiles))]
    public async Task FamilyLoadOptions_WithFamilySourceFamily_LoadsFamily(string familyPath)
    {
        // Arrange
        var document = Application.NewProjectDocument(UnitSystem.Metric);

        try
        {
            var options = new FamilyLoadOptions(overwrite: true, familySource: FamilySource.Family);

            // Act
            using var transaction = new Transaction(document, "Load Family");
            transaction.Start();

            var result = document.LoadFamily(familyPath, options, out var family);

            transaction.Commit();

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(result).IsTrue();
                await Assert.That(family).IsNotNull();
            }
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRfaFiles))]
    public async Task FamilyLoadOptions_LoadedFamilyHasSymbols(string familyPath)
    {
        // Arrange
        var document = Application.NewProjectDocument(UnitSystem.Metric);

        try
        {
            var options = new FamilyLoadOptions();

            // Act
            using var transaction = new Transaction(document, "Load Family");
            transaction.Start();

            document.LoadFamily(familyPath, options, out var family);

            transaction.Commit();

            var symbolIds = family.GetFamilySymbolIds();

            // Assert
            await Assert.That(symbolIds.Count).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task FamilyLoadOptions_MultipleFamilies_AllLoad()
    {
        // Arrange
        var document = Application.NewProjectDocument(UnitSystem.Metric);
        var familyPaths = GetSampleRfaFiles().Take(3).ToList();

        if (familyPaths.Count == 0)
        {
            Skip.Test("No sample files available");
            return;
        }

        try
        {
            var options = new FamilyLoadOptions();
            var loadedCount = 0;

            // Act
            using var transaction = new Transaction(document, "Load Multiple Families");
            transaction.Start();

            foreach (var path in familyPaths)
            {
                if (document.LoadFamily(path, options, out _)) loadedCount++;
            }

            transaction.Commit();

            // Assert
            await Assert.That(loadedCount).IsEqualTo(familyPaths.Count);
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRfaFiles))]
    public async Task FamilyLoadOptions_TransactionCommits_Successfully(string familyPath)
    {
        // Arrange
        var document = Application.NewProjectDocument(UnitSystem.Metric);

        try
        {
            var options = new FamilyLoadOptions();

            // Act
            using var transaction = new Transaction(document, "Load Family");
            transaction.Start();

            document.LoadFamily(familyPath, options, out _);

            var status = transaction.Commit();

            // Assert
            await Assert.That(status).IsEqualTo(TransactionStatus.Committed);
        }
        finally
        {
            document.Close(false);
        }
    }
}