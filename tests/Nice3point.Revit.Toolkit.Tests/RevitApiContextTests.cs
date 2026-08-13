namespace Nice3point.Revit.Toolkit.Tests;

public sealed class RevitApiContextTests : RevitApiTest
{
    private Document _document = null!;
    private Level _level = null!;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
        _level = (Level)_document.CollectElements()
            .Instances()
            .OfCategory(BuiltInCategory.OST_Levels)
            .First();
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument()
    {
        _document.Close(false);
    }

    [Test]
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
    public async Task BeginFailureSuppressionScope_WithoutScope_RollsBackOnError()
    {
        // Act
        using var transaction = new Transaction(_document, "Create overlapping walls");
        transaction.Start();

        CreateOverlappingWalls(_document, _level);

        var status = transaction.Commit();

        // Assert
        await Assert.That(status).IsEqualTo(TransactionStatus.RolledBack);
    }

    [Test]
    public async Task BeginFailureSuppressionScope_WithResolveErrors_CommitsTransaction()
    {
        // Arrange
        using (RevitApiContext.BeginFailureSuppressionScope(true))
        {
            // Act
            using var transaction = new Transaction(_document, "Create overlapping walls");
            transaction.Start();

            CreateOverlappingWalls(_document, _level);

            var status = transaction.Commit();

            // Assert
            await Assert.That(status).IsEqualTo(TransactionStatus.Committed);
        }
    }

    [Test]
    public async Task BeginFailureSuppressionScope_NestedScopes_CommitsTransaction()
    {
        // Arrange
        using (RevitApiContext.BeginFailureSuppressionScope())
        using (RevitApiContext.BeginFailureSuppressionScope())
        {
            // Act
            using var transaction = new Transaction(_document, "Create overlapping walls");
            transaction.Start();

            CreateOverlappingWalls(_document, _level);

            var status = transaction.Commit();

            // Assert
            await Assert.That(status).IsEqualTo(TransactionStatus.Committed);
        }
    }

    [Test]
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
    public async Task BeginFailureSuppressionScope_WithResolveErrorsFalse_RollsBackOnError()
    {
        // Arrange
        using (RevitApiContext.BeginFailureSuppressionScope(false))
        {
            // Act
            using var transaction = new Transaction(_document, "Create overlapping walls");
            transaction.Start();

            CreateOverlappingWalls(_document, _level);

            var status = transaction.Commit();

            // Assert
            await Assert.That(status).IsEqualTo(TransactionStatus.RolledBack);
        }
    }

    [Test]
    public async Task BeginFailureSuppressionScope_MultipleTransactions_AllCommit()
    {
        // Arrange
        var committedCount = 0;

        // Act
        using (RevitApiContext.BeginFailureSuppressionScope())
        {
            for (var i = 0; i < 3; i++)
            {
                using var transaction = new Transaction(_document, $"Transaction {i}");
                transaction.Start();

                Wall.Create(_document, Line.CreateBound(new XYZ(0, i * 5, 0), new XYZ(10, i * 5, 0)), _level.Id, false);

                if (transaction.Commit() == TransactionStatus.Committed)
                {
                    committedCount++;
                }
            }
        }

        // Assert
        await Assert.That(committedCount).IsEqualTo(3);
    }

    private static void CreateOverlappingWalls(Document document, Level level)
    {
        Wall.Create(document, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), level.Id, false);
        Wall.Create(document, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), level.Id, false);
    }
}
