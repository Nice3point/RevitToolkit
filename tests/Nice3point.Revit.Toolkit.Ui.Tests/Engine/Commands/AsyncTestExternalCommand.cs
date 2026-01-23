using Nice3point.Revit.Toolkit.External;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Engine.Commands;

/// <summary>
///     Base class for asynchronous test commands.
/// </summary>
[PublicAPI]
public abstract class AsyncTestExternalCommand : AsyncExternalCommand
{
    protected TestRunner Runner { get; private set; } = null!;

    public sealed override async Task ExecuteAsync()
    {
        Runner = new TestRunner(GetType().Name);

        try
        {
            await TestAsync();
        }
        catch (Exception exception)
        {
            Runner.Exception = exception;
        }

        Runner.ShowResult();
    }

    /// <summary>
    ///     Override this method to implement async test logic.
    /// </summary>
    public abstract Task TestAsync();
}