using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class RevitApiContextTests : RevitApiTest
{
    private static readonly string TemplatesPath = $@"C:\ProgramData\Autodesk\RVT {Application.VersionNumber}\Family Templates\English\Metric Generic Model.rft";

    [Before(Class)]
    public static void ValidateTemplates()
    {
        if (!File.Exists(TemplatesPath))
        {
            Skip.Test($"Family template not found at {TemplatesPath}");
        }
    }

    public static IEnumerable<string> GetFamilyTemplate()
    {
        if (!File.Exists(TemplatesPath))
        {
            yield return string.Empty;
            yield break;
        }

        yield return TemplatesPath;
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Application_WhenAccessed_ReturnsValidApplication()
    {
        // Act
        var application = RevitApiContext.Application;

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(application).IsNotNull();
            await Assert.That(application.IsValidObject).IsTrue();
            await Assert.That(application.VersionNumber).IsNotEmpty();
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetFamilyTemplate))]
    public async Task BeginFailureSuppressionScope_WithResolveErrors_CommitsTransaction(string templatePath)
    {
        // Arrange
        var document = Application.NewFamilyDocument(templatePath);

        try
        {
            // Act
            using (RevitApiContext.BeginFailureSuppressionScope(resolveErrors: true))
            {
                using var transaction = new Transaction(document, "Create Reference Planes");
                transaction.Start();

                document.FamilyCreate.NewReferencePlane(new XYZ(-10, 0, 0), new XYZ(10, 0, 0), XYZ.BasisZ, document.ActiveView);
                document.FamilyCreate.NewReferencePlane(new XYZ(-10, 0, 0), new XYZ(10, 0, 0), XYZ.BasisZ, document.ActiveView);

                var status = transaction.Commit();

                // Assert
                await Assert.That(status).IsEqualTo(TransactionStatus.Committed);
            }
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetFamilyTemplate))]
    public async Task BeginFailureSuppressionScope_NestedScopes_CommitsTransaction(string templatePath)
    {
        // Arrange
        var document = Application.NewFamilyDocument(templatePath);

        try
        {
            // Act
            using (RevitApiContext.BeginFailureSuppressionScope())
            {
                using (RevitApiContext.BeginFailureSuppressionScope())
                {
                    using var transaction = new Transaction(document, "Nested Scope Test");
                    transaction.Start();

                    document.FamilyCreate.NewReferencePlane(new XYZ(-5, 0, 0), new XYZ(5, 0, 0), XYZ.BasisZ, document.ActiveView);

                    var status = transaction.Commit();

                    // Assert
                    await Assert.That(status).IsEqualTo(TransactionStatus.Committed);
                }
            }
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task BeginFailureSuppressionScope_DisposedTwice_DoesNotThrow()
    {
        // Arrange
        var scope = RevitApiContext.BeginFailureSuppressionScope();

        // Act & Assert
        await Assert.That(() =>
        {
            scope.Dispose();
            scope.Dispose();
        }).ThrowsNothing();
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetFamilyTemplate))]
    public async Task BeginFailureSuppressionScope_WithResolveErrorsFalse_CompletesTransaction(string templatePath)
    {
        // Arrange
        var document = Application.NewFamilyDocument(templatePath);

        try
        {
            // Act
            using (RevitApiContext.BeginFailureSuppressionScope(resolveErrors: false))
            {
                using var transaction = new Transaction(document, "Test with dismiss");
                transaction.Start();

                document.FamilyCreate.NewReferencePlane(new XYZ(-5, 0, 0), new XYZ(5, 0, 0), XYZ.BasisZ, document.ActiveView);

                var status = transaction.Commit();

                // Assert
                await Assert.That(status).IsEqualTo(TransactionStatus.Committed);
            }
        }
        finally
        {
            document.Close(false);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    [MethodDataSource(nameof(GetFamilyTemplate))]
    public async Task BeginFailureSuppressionScope_MultipleTransactions_AllCommit(string templatePath)
    {
        // Arrange
        var document = Application.NewFamilyDocument(templatePath);
        var committedCount = 0;

        try
        {
            // Act
            using (RevitApiContext.BeginFailureSuppressionScope())
            {
                for (var i = 0; i < 3; i++)
                {
                    using var transaction = new Transaction(document, $"Transaction {i}");
                    transaction.Start();

                    document.FamilyCreate.NewReferencePlane(
                        new XYZ(-5, i * 2, 0),
                        new XYZ(5, i * 2, 0),
                        XYZ.BasisZ,
                        document.ActiveView);

                    if (transaction.Commit() == TransactionStatus.Committed) committedCount++;
                }
            }

            // Assert
            await Assert.That(committedCount).IsEqualTo(3);
        }
        finally
        {
            document.Close(false);
        }
    }
}