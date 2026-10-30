using System.Diagnostics.CodeAnalysis;
using System.Windows.Threading;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;
using InvalidOperationException = Autodesk.Revit.Exceptions.InvalidOperationException;

namespace Nice3point.Revit.Toolkit.Tests;

[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
public sealed class ExternalEventHandlerTests : RevitApiUiTest
{
    private const string WrittenResult = "Written";
    private static readonly Guid AddInId = new(TestAddIn.Id);

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_CreatedInsideAddInCallback_RunsTheHandlerAsTheAddIn(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<Guid?>(ReadActiveAddInId);

        // Act
        var handlerAddInId = await externalEvent.RaiseAsync().WaitAsync(cancellationToken);

        // Assert
        await Assert.That(handlerAddInId).IsEqualTo(AddInId);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_CreatedInsideAddInCallbackAndRaisedFromThreadPool_RunsTheHandlerAsTheAddIn(CancellationToken cancellationToken)
    {
        // Arrange
        var externalEvent = new AsyncRequestExternalEvent<Guid?>(ReadActiveAddInId);

        // Act
        var handlerAddInId = await Task.Run(externalEvent.RaiseAsync, cancellationToken).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(handlerAddInId).IsEqualTo(AddInId);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_CreatedOnThreadPool_RunsTheHandlerAsTheAddIn(CancellationToken cancellationToken)
    {
        // Arrange
        CreateExternalEventInsideAddInCallback();
        var (creationAddInId, externalEvent) = await Task.Run(() => (ReadActiveAddInId(), new AsyncRequestExternalEvent<Guid?>(ReadActiveAddInId)), cancellationToken);

        // Act
        var handlerAddInId = await externalEvent.RaiseAsync().WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(creationAddInId).IsNotEqualTo(AddInId).Because("the add-in is not active on a thread-pool thread");
            await Assert.That(handlerAddInId).IsEqualTo(AddInId);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_CreatedInDispatcherCallback_RunsTheHandlerAsTheAddIn(CancellationToken cancellationToken)
    {
        // Arrange
        CreateExternalEventInsideAddInCallback();
        var dispatcher = Dispatcher.CurrentDispatcher;
        var (creationAddInId, externalEvent) = await dispatcher.InvokeAsync(() => (ReadActiveAddInId(), new AsyncRequestExternalEvent<Guid?>(ReadActiveAddInId))).Task.WaitAsync(cancellationToken);

        // Act
        var handlerAddInId = await externalEvent.RaiseAsync().WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(creationAddInId).IsNotEqualTo(AddInId).Because("the add-in is not active in a dispatcher callback outside a Revit callback");
            await Assert.That(handlerAddInId).IsEqualTo(AddInId);
        }
    }

    [Test]
    [Timeout(10_000)]
    public async Task Raise_HandlerTypeCreatedOnThreadPool_RunsTheHandlerAsTheAddIn(CancellationToken cancellationToken)
    {
        // Arrange
        CreateExternalEventInsideAddInCallback();
        var handler = await Task.Run(() => new ActiveAddInHandler(), cancellationToken);

        // Act
        handler.Raise();
        var handlerAddInId = await handler.Execution.Task.WaitAsync(cancellationToken);

        // Assert
        await Assert.That(handlerAddInId).IsEqualTo(AddInId);
    }

    [Test]
    [Timeout(30_000)]
    public async Task RaiseAsync_CreatedOnThreadPool_WritesAnEntityOfTheVendorSchema(CancellationToken cancellationToken)
    {
        // Arrange
        CreateExternalEventInsideAddInCallback();
        var externalEvent = await Task.Run(() => new AsyncRequestExternalEvent<string>(WriteVendorEntity), cancellationToken);

        // Act
        var result = await externalEvent.RaiseAsync().WaitAsync(cancellationToken);

        // Assert
        await Assert.That(result).IsEqualTo(WrittenResult);
    }

    // The toolkit captures the add-in of the test assembly from an external event created inside a Revit callback.
    private static void CreateExternalEventInsideAddInCallback()
    {
        _ = new External.ExternalEvent(static () => { });
    }

    private static Guid? ReadActiveAddInId()
    {
        return RevitApiContext.Application.ActiveAddInId?.GetGUID();
    }

    // Writes an entity of a schema that grants write access to the vendor of the test add-in alone.
    // Returns the written result, or the message Revit refuses the write with.
    private static string WriteVendorEntity(UIApplication uiApplication)
    {
        var schemaId = Guid.NewGuid();
        var schemaBuilder = new SchemaBuilder(schemaId);
        schemaBuilder.SetSchemaName($"ToolkitTests{schemaId:N}");
        schemaBuilder.SetVendorId(TestAddIn.VendorId);
        schemaBuilder.SetReadAccessLevel(AccessLevel.Public);
        schemaBuilder.SetWriteAccessLevel(AccessLevel.Vendor);
        schemaBuilder.AddSimpleField("Value", typeof(string));
        var schema = schemaBuilder.Finish();

        var document = uiApplication.Application.NewProjectDocument(UnitSystem.Metric);
        try
        {
            using var transaction = new Transaction(document, "Write entity");
            transaction.Start();

            var entity = new Entity(schema);
            entity.Set("Value", WrittenResult);
            document.ProjectInformation.SetEntity(entity);

            transaction.Commit();
            return WrittenResult;
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
        finally
        {
            document.Close(false);
        }
    }

    private sealed class ActiveAddInHandler : ExternalEventHandler
    {
        public TaskCompletionSource<Guid?> Execution { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override void Execute(UIApplication uiApplication)
        {
            Execution.TrySetResult(uiApplication.ActiveAddInId?.GetGUID());
        }
    }
}
