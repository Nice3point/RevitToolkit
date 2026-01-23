using Nice3point.Revit.Toolkit.External;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Engine.Commands;

/// <summary>
///     Base class for synchronous test commands.
/// </summary>
[PublicAPI]
public abstract class TestExternalCommand : ExternalCommand
{
    protected TestRunner Runner { get; private set; } = null!;

    public sealed override void Execute()
    {
        Runner = new TestRunner(GetType().Name);

        try
        {
            Test();
        }
        catch (Exception exception)
        {
            Runner.Exception = exception;
        }

        Runner.ShowResult();
    }

    /// <summary>
    ///     Override this method to implement test logic.
    /// </summary>
    public abstract void Test();
}