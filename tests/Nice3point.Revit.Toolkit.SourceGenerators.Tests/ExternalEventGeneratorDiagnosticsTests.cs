using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

[NotInParallel("GeneratorSnapshots")]
public sealed class ExternalEventGeneratorDiagnosticsTests
{
    [Test]
    public async Task GenericMethod_ReportsRVTTK0003Async()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run<TValue>(TValue value) { }
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source, ["RVTTK0003"]);

        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Verify(driver.GetRunResult());
    }

    [Test]
    [Arguments("Task", "System.Threading.Tasks.Task", "System.Threading.Tasks.Task.CompletedTask")]
    [Arguments("TaskOfResult", "System.Threading.Tasks.Task<int>", "System.Threading.Tasks.Task.FromResult(42)")]
    public async Task TaskReturn_ReportsRVTTK0001Async(string scenario, string returnType, string expression)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial class Model
                       {
                           [ExternalEvent]
                           private {{returnType}} Run() => {{expression}};
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source, ["RVTTK0001"]);

        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Verify(driver.GetRunResult()).UseParameters(scenario);
    }

    [Test]
    public async Task AnnotatedOverloads_ReportRVTTK0004ForEachMethodAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run() { }

                                  [ExternalEvent]
                                  private void Run(int quantity) { }
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source, ["RVTTK0004", "RVTTK0004"]);

        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Verify(driver.GetRunResult());
    }
}
