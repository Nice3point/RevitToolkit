using System.Diagnostics.CodeAnalysis;
using System.Windows.Threading;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;
using InvalidOperationException = Autodesk.Revit.Exceptions.InvalidOperationException;

namespace Nice3point.Revit.Toolkit.Tests;

[SuppressMessage("ReSharper", "MethodSupportsCancellation", Justification = "The tests raise the event through the overload without a token and bound the wait with WaitAsync.")]
public sealed class ExternalEventHandlerTests : RevitApiUiTest
{
    private const string WrittenResult = "Written";
    private static readonly Guid AddInId = new(TestsConfiguration.AddInId);

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
        var (creationAddInId, externalEvent) = await Task.Run(CreateActiveAddInIdEvent, cancellationToken);

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
        var creation = dispatcher.InvokeAsync(CreateActiveAddInIdEvent);
        var (creationAddInId, externalEvent) = await creation.Task.WaitAsync(cancellationToken);

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
        var handler = await Task.Run(static () => new ActiveAddInHandler(), cancellationToken);

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
        var externalEvent = await Task.Run(static () => new AsyncRequestExternalEvent<string>(WriteVendorEntity), cancellationToken);

        // Act
        var result = await externalEvent.RaiseAsync().WaitAsync(cancellationToken);

        // Assert
        await Assert.That(result).IsEqualTo(WrittenResult);
    }

    [Test]
    [Timeout(10_000)]
    public async Task RaiseAsync_AssemblyRegisteredAndEventCreatedOnThreadPool_RunsTheHandlerAsTheAddIn(CancellationToken cancellationToken)
    {
        // Arrange
        UiApplication.RegisterAddInAssembly(typeof(ExternalEventHandlerTests).Assembly);
        var (creationAddInId, externalEvent) = await Task.Run(CreateActiveAddInIdEvent, cancellationToken);

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
    public async Task RegisterAddInAssembly_OnThreadPool_ThrowsInvalidOperation(CancellationToken cancellationToken)
    {
        // Arrange
        var assembly = typeof(ExternalEventHandlerTests).Assembly;

        // Act
        var registration = Task.Run(() => UiApplication.RegisterAddInAssembly(assembly), cancellationToken);

        // Assert
        await Assert.That(() => registration).Throws<System.InvalidOperationException>();
    }

    [Test]
    public async Task RegisterAddInAssembly_NullAssembly_ThrowsArgumentNull()
    {
        await Assert.That(() => UiApplication.RegisterAddInAssembly(null!)).Throws<System.ArgumentNullException>();
    }

    /// <summary>
    ///     Captures the add-in of the test assembly through an external event created inside a Revit callback.
    /// </summary>
    private static void CreateExternalEventInsideAddInCallback()
    {
        _ = new External.ExternalEvent(static () => { });
    }

    private static (Guid? CreationAddInId, AsyncRequestExternalEvent<Guid?> ExternalEvent) CreateActiveAddInIdEvent()
    {
        return (ReadActiveAddInId(), new AsyncRequestExternalEvent<Guid?>(ReadActiveAddInId));
    }

    private static Guid? ReadActiveAddInId()
    {
        return RevitApiContext.Application.ActiveAddInId?.GetGUID();
    }

    /// <summary>
    ///     Writes an entity of a schema with the vendor write access level and the vendor ID of the test add-in.
    /// </summary>
    /// <param name="uiApplication">The current <see cref="UIApplication" />.</param>
    /// <returns>The written result, or the message Revit refuses the write with.</returns>
    private static string WriteVendorEntity(UIApplication uiApplication)
    {
        var schemaId = Guid.NewGuid();
        var schemaBuilder = new SchemaBuilder(schemaId);
        schemaBuilder.SetSchemaName($"ToolkitTests{schemaId:N}");
        schemaBuilder.SetVendorId(TestsConfiguration.VendorId);
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
