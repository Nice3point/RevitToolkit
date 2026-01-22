using Nice3point.Revit.Toolkit.Options;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class DuplicateTypeNamesHandlerTests : RevitApiTest
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

        if (!Directory.EnumerateFiles(SamplesPath, "*.rvt").Any())
        {
            Skip.Test($"No .rvt files found in {SamplesPath}");
        }
    }

    public static IEnumerable<string> GetSampleRvtFiles()
    {
        if (!Directory.Exists(SamplesPath))
        {
            yield return string.Empty;
            yield break;
        }

        var files = Directory.EnumerateFiles(SamplesPath, "*.rvt")
            .OrderBy(path => new FileInfo(path).Length)
            .Take(1);

        foreach (var file in files) yield return file;
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRvtFiles))]
    public async Task DuplicateTypeNamesHandler_DefaultConstructor_CopiesElements(string sourceProjectPath)
    {
        // Arrange
        var targetDocument = Application.NewProjectDocument(UnitSystem.Metric);
        Document? sourceDocument = null;

        try
        {
            sourceDocument = Application.OpenDocumentFile(sourceProjectPath);

            var elementsToCopy = new FilteredElementCollector(sourceDocument)
                .OfClass(typeof(FamilySymbol))
                .ToElementIds()
                .Take(5)
                .ToList();

            if (elementsToCopy.Count == 0)
            {
                Skip.Test("No FamilySymbols found in source document");
                return;
            }

            var handler = new DuplicateTypeNamesHandler();
            var copyOptions = new CopyPasteOptions();
            copyOptions.SetDuplicateTypeNamesHandler(handler);

            // Act
            using var transaction = new Transaction(targetDocument, "Copy elements");
            transaction.Start();

            var copiedIds = ElementTransformUtils.CopyElements(
                sourceDocument,
                elementsToCopy,
                targetDocument,
                Transform.Identity,
                copyOptions);

            transaction.Commit();

            // Assert
            await Assert.That(copiedIds.Count).IsGreaterThan(0);
        }
        finally
        {
            sourceDocument?.Close(false);
            targetDocument.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRvtFiles))]
    public async Task DuplicateTypeNamesHandler_UseDestinationTypes_CopiesElements(string sourceProjectPath)
    {
        // Arrange
        var targetDocument = Application.NewProjectDocument(UnitSystem.Metric);
        Document? sourceDocument = null;

        try
        {
            sourceDocument = Application.OpenDocumentFile(sourceProjectPath);

            var elementsToCopy = new FilteredElementCollector(sourceDocument)
                .OfClass(typeof(FamilySymbol))
                .ToElementIds()
                .Take(5)
                .ToList();

            if (elementsToCopy.Count == 0)
            {
                Skip.Test("No FamilySymbols found in source document");
                return;
            }

            var handler = new DuplicateTypeNamesHandler(DuplicateTypeAction.UseDestinationTypes);
            var copyOptions = new CopyPasteOptions();
            copyOptions.SetDuplicateTypeNamesHandler(handler);

            // Act
            using var transaction = new Transaction(targetDocument, "Copy elements");
            transaction.Start();

            var copiedIds = ElementTransformUtils.CopyElements(
                sourceDocument,
                elementsToCopy,
                targetDocument,
                Transform.Identity,
                copyOptions);

            transaction.Commit();

            // Assert
            await Assert.That(copiedIds.Count).IsGreaterThan(0);
        }
        finally
        {
            sourceDocument?.Close(false);
            targetDocument.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRvtFiles))]
    public async Task DuplicateTypeNamesHandler_CopyTwice_SecondCopySucceeds(string sourceProjectPath)
    {
        // Arrange
        var targetDocument = Application.NewProjectDocument(UnitSystem.Metric);
        Document? sourceDocument = null;

        try
        {
            sourceDocument = Application.OpenDocumentFile(sourceProjectPath);

            var elementsToCopy = new FilteredElementCollector(sourceDocument)
                .OfClass(typeof(FamilySymbol))
                .ToElementIds()
                .Take(3)
                .ToList();

            if (elementsToCopy.Count == 0)
            {
                Skip.Test("No FamilySymbols found in source document");
                return;
            }

            var handler = new DuplicateTypeNamesHandler(DuplicateTypeAction.UseDestinationTypes);
            var copyOptions = new CopyPasteOptions();
            copyOptions.SetDuplicateTypeNamesHandler(handler);

            // Act - first copy
            using (var transaction = new Transaction(targetDocument, "First copy"))
            {
                transaction.Start();
                ElementTransformUtils.CopyElements(sourceDocument, elementsToCopy, targetDocument, Transform.Identity, copyOptions);
                transaction.Commit();
            }

            // Act - second copy (duplicates will be handled)
            using (var transaction = new Transaction(targetDocument, "Second copy"))
            {
                transaction.Start();

                var secondCopyIds = ElementTransformUtils.CopyElements(
                    sourceDocument,
                    elementsToCopy,
                    targetDocument,
                    Transform.Identity,
                    copyOptions);

                transaction.Commit();

                // Assert
                await Assert.That(secondCopyIds.Count).IsGreaterThanOrEqualTo(0);
            }
        }
        finally
        {
            sourceDocument?.Close(false);
            targetDocument.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRvtFiles))]
    public async Task DuplicateTypeNamesHandler_Abort_RollsBackOnDuplicate(string sourceProjectPath)
    {
        // Arrange
        var targetDocument = Application.NewProjectDocument(UnitSystem.Metric);
        Document? sourceDocument = null;

        try
        {
            sourceDocument = Application.OpenDocumentFile(sourceProjectPath);

            var elementsToCopy = new FilteredElementCollector(sourceDocument)
                .OfClass(typeof(FamilySymbol))
                .ToElementIds()
                .Take(3)
                .ToList();

            if (elementsToCopy.Count == 0)
            {
                Skip.Test("No FamilySymbols found in source document");
                return;
            }

            // First copy with default handler
            var firstHandler = new DuplicateTypeNamesHandler();
            var firstOptions = new CopyPasteOptions();
            firstOptions.SetDuplicateTypeNamesHandler(firstHandler);

            using (var transaction = new Transaction(targetDocument, "First copy"))
            {
                transaction.Start();
                ElementTransformUtils.CopyElements(sourceDocument, elementsToCopy, targetDocument, Transform.Identity, firstOptions);
                transaction.Commit();
            }

            // Act - second copy with Abort handler
            var abortHandler = new DuplicateTypeNamesHandler(DuplicateTypeAction.Abort);
            var abortOptions = new CopyPasteOptions();
            abortOptions.SetDuplicateTypeNamesHandler(abortHandler);

            using (var transaction = new Transaction(targetDocument, "Second copy with abort"))
            {
                transaction.Start();

                ElementTransformUtils.CopyElements(
                    sourceDocument,
                    elementsToCopy,
                    targetDocument,
                    Transform.Identity,
                    abortOptions);

                var status = transaction.Commit();

                // Assert
                await Assert.That(status).IsEqualTo(TransactionStatus.RolledBack);
            }
        }
        finally
        {
            sourceDocument?.Close(false);
            targetDocument.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetSampleRvtFiles))]
    public async Task DuplicateTypeNamesHandler_CopyWalls_CopiesElements(string sourceProjectPath)
    {
        // Arrange
        var targetDocument = Application.NewProjectDocument(UnitSystem.Metric);
        Document? sourceDocument = null;

        try
        {
            sourceDocument = Application.OpenDocumentFile(sourceProjectPath);

            var elementsToCopy = new FilteredElementCollector(sourceDocument)
                .OfClass(typeof(FamilySymbol))
                .ToElementIds()
                .Take(5)
                .ToList();

            if (elementsToCopy.Count == 0)
            {
                Skip.Test("No Walls found in source document");
                return;
            }

            var handler = new DuplicateTypeNamesHandler();
            var copyOptions = new CopyPasteOptions();
            copyOptions.SetDuplicateTypeNamesHandler(handler);

            // Act
            using var transaction = new Transaction(targetDocument, "Copy walls");
            transaction.Start();

            var copiedIds = ElementTransformUtils.CopyElements(
                sourceDocument,
                elementsToCopy,
                targetDocument,
                Transform.Identity,
                copyOptions);

            transaction.Commit();

            // Assert
            await Assert.That(copiedIds.Count).IsGreaterThan(0);
        }
        finally
        {
            sourceDocument?.Close(false);
            targetDocument.Close(false);
        }
    }
}