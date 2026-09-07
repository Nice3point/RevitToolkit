using Microsoft.CodeAnalysis.CSharp;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

[Category("Regression")]
[NotInParallel("GeneratorSnapshots")]
public sealed class ExternalEventGeneratorRegressionTests
{
    [Test]
    public async Task GenericContainingType_PreservesParametersAndConstraintsAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model<TValue> where TValue : class, new()
                              {
                                  [ExternalEvent]
                                  private TValue Run(TValue value) => value;

                                  public System.Threading.Tasks.Task<TValue> UseGeneratedMembers(TValue value)
                                      => RunAsyncEvent.RaiseAsync(value);
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult());
    }

    [Test]
    public async Task GenericArguments_PreserveExtensionConstraintsAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model<TValue> where TValue : class, System.IDisposable, new()
                              {
                                  [ExternalEvent]
                                  private void Run(TValue value, int quantity) { }

                                  public void UseGeneratedMembers(TValue value)
                                  {
                                      _ = RunEvent.Raise(value, 42);
                                      _ = RunAsyncEvent.RaiseAsync(value, 42);
                                  }
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult());
    }

    [Test]
    public async Task NestedGenericType_PreservesHierarchyAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Outer<TValue> where TValue : class
                              {
                                  public partial class Model<TResult> where TResult : struct
                                  {
                                      [ExternalEvent]
                                      private TResult Run(TValue value, TResult result) => result;

                                      public System.Threading.Tasks.Task<TResult> UseGeneratedMembers(TValue value, TResult result)
                                          => RunAsyncEvent.RaiseAsync(value, result);
                                  }
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult());
    }

    [Test]
    public async Task SameNameWithDifferentArity_GeneratesDistinctFilesAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run() { }

                                  public void UseGeneratedMembers() => RunEvent.Raise();
                              }

                              public partial class Model<TValue>
                              {
                                  [ExternalEvent]
                                  private void Run() { }

                                  public void UseGeneratedMembers() => RunEvent.Raise();
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);
        var sources = driver.GetRunResult().Results.Single().GeneratedSources;

        await Assert.That(sources).Count().IsEqualTo(2);
        await Assert.That(sources.Select(static result => result.HintName).Distinct()).Count().IsEqualTo(2);
        await Verify(driver.GetRunResult());
    }

    [Test]
    [Arguments("Method", "Model", "@event", "int quantity, string label", "eventEvent")]
    [Arguments("Type", "@class", "Run", "int quantity, string label", "RunEvent")]
    [Arguments("Parameter", "Model", "Run", "int @event, string label", "RunEvent")]
    public async Task EscapedIdentifiers_GenerateCompilableEventsAsync(string scenario, string typeName, string methodName, string parameters, string eventName)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial class {{typeName}}
                       {
                           [ExternalEvent]
                           private void {{methodName}}({{parameters}}) { }

                           public void UseGeneratedMembers() => {{eventName}}.Raise(42, "Walls");
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult()).UseParameters(scenario);
    }

    [Test]
    public async Task PrivateNestedType_KeepsArgumentHelpersAccessibleAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Outer
                              {
                                  private partial class Model
                                  {
                                      [ExternalEvent]
                                      private void Run(int quantity, string label) { }

                                      public void UseGeneratedMembers()
                                      {
                                          _ = RunEvent.Raise(new RunArgs(42, "Walls"));
                                          _ = RunAsyncEvent.RaiseAsync(new RunArgs(42, "Walls"));
                                      }
                                  }
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult());
    }

    [Test]
    [Arguments("struct", LanguageVersion.CSharp13)]
    [Arguments("struct", LanguageVersion.CSharp14)]
    [Arguments("record struct", LanguageVersion.CSharp13)]
    [Arguments("record struct", LanguageVersion.CSharp14)]
    public async Task StructInstanceMethod_WithMultipleArgumentsGeneratesCompilableEventsAsync(string declaration, LanguageVersion languageVersion)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial {{declaration}} Model
                       {
                           [ExternalEvent]
                           private void Run(int quantity, string label) { }

                           public void UseGeneratedMembers() => RunEvent.Raise(42, "Walls");
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source, languageVersion: languageVersion);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult()).UseParameters(declaration, languageVersion);
    }

    [Test]
    [Arguments("Capitalization", "int value, int Value")]
    [Arguments("ExtensionReceiver", "int externalEvent, int quantity")]
    public async Task ParameterNames_PreserveDistinctBindingsAsync(string scenario, string parameters)
    {
        var arguments = scenario switch
        {
            "Capitalization" => "value: 42, Value: 7",
            _ => "externalEvent: 42, quantity: 7"
        };

        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial class Model
                       {
                           [ExternalEvent]
                           private void Run({{parameters}}) { }

                           public void UseGeneratedMembers() => RunEvent.Raise({{arguments}});
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult()).UseParameters(scenario);
    }
}
