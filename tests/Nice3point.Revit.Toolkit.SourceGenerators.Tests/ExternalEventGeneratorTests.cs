using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

[NotInParallel("GeneratorSnapshots")]
public sealed class ExternalEventGeneratorTests
{
    [Test]
    [Arguments("Void", false, false, 0)]
    [Arguments("VoidApplication", false, true, 0)]
    [Arguments("VoidArgument", false, false, 1)]
    [Arguments("VoidApplicationArgument", false, true, 1)]
    [Arguments("VoidArguments", false, false, 2)]
    [Arguments("VoidApplicationArguments", false, true, 2)]
    [Arguments("Result", true, false, 0)]
    [Arguments("ResultApplication", true, true, 0)]
    [Arguments("ResultArgument", true, false, 1)]
    [Arguments("ResultApplicationArgument", true, true, 1)]
    [Arguments("ResultArguments", true, false, 2)]
    [Arguments("ResultApplicationArguments", true, true, 2)]
    public async Task MethodSignature_GeneratesCompilableEventsAsync(string scenario, bool returnsValue, bool hasApplication, int argumentCount)
    {
        var parameters = new List<string>();
        if (hasApplication)
        {
            parameters.Add("Autodesk.Revit.UI.UIApplication application");
        }

        if (argumentCount > 0)
        {
            parameters.Add("int quantity");
        }

        if (argumentCount > 1)
        {
            parameters.Add("string label");
        }

        var arguments = argumentCount switch
        {
            0 => string.Empty,
            1 => "42",
            _ => "42, \"Walls\""
        };

        var returnType = returnsValue ? "int" : "void";
        var body = returnsValue ? "=> 42;" : "{ }";
        var synchronousCall = returnsValue ? string.Empty : $"_ = RunEvent.Raise({arguments});";
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial class Model
                       {
                           [ExternalEvent]
                           private {{returnType}} Run({{string.Join(", ", parameters)}}) {{body}}

                           public void UseGeneratedMembers()
                           {
                               {{synchronousCall}}
                               _ = RunAsyncEvent.RaiseAsync({{arguments}});
                           }
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult()).UseParameters(scenario);
    }

    [Test]
    public async Task NullableSignature_PreservesAnnotationsAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private string? Run(string? label, int? quantity) => label;

                                  public System.Threading.Tasks.Task<string?> UseGeneratedMembers()
                                      => RunAsyncEvent.RaiseAsync(null, null);
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);
        await Verify(driver.GetRunResult());
    }

    [Test]
    [Arguments("PublicClass")]
    [Arguments("InternalClass")]
    [Arguments("NestedClass")]
    [Arguments("InternalNestedClass")]
    [Arguments("StaticClass")]
    [Arguments("StaticMethod")]
    [Arguments("Struct")]
    [Arguments("RecordClass")]
    [Arguments("RecordStruct")]
    public async Task ContainingType_PreservesDeclarationAsync(string scenario)
    {
        var declaration = scenario switch
        {
            "InternalClass" => "internal partial class Model",
            "InternalNestedClass" => "internal partial class Model",
            "StaticClass" => "public static partial class Model",
            "Struct" => "public partial struct Model",
            "RecordClass" => "public partial record Model",
            "RecordStruct" => "public partial record struct Model",
            _ => "public partial class Model"
        };

        var isNested = scenario is "NestedClass" or "InternalNestedClass";
        var staticModifier = scenario is "StaticClass" or "StaticMethod" ? "static " : string.Empty;
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       {{(isNested ? "public partial class Outer {" : string.Empty)}}
                       {{declaration}}
                       {
                           [ExternalEvent]
                           private {{staticModifier}}void Run() { }

                           public {{staticModifier}}void UseGeneratedMembers()
                           {
                               _ = RunEvent.Raise();
                               _ = RunAsyncEvent.RaiseAsync();
                           }
                       }
                       {{(isNested ? "}" : string.Empty)}}
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Verify(driver.GetRunResult()).UseParameters(scenario);
    }

    [Test]
    [Arguments("DirectDisabled", false, false)]
    [Arguments("DirectEnabled", true, false)]
    [Arguments("RecordDisabled", false, true)]
    [Arguments("RecordEnabled", true, true)]
    public async Task AllowDirectInvocation_PreservesOptionsAsync(string scenario, bool allowDirectInvocation, bool multipleArguments)
    {
        var parameters = multipleArguments ? "int quantity, string label" : "int quantity";
        var arguments = multipleArguments ? "42, \"Walls\"" : "42";
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial class Model
                       {
                           [ExternalEvent(AllowDirectInvocation = {{allowDirectInvocation.ToString().ToLowerInvariant()}})]
                           private void Run({{parameters}}) { }

                           public void UseGeneratedMembers()
                           {
                               _ = RunEvent.Raise({{arguments}});
                               _ = RunAsyncEvent.RaiseAsync({{arguments}});
                           }
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Verify(driver.GetRunResult()).UseParameters(scenario);
    }

    [Test]
    [Arguments(LanguageVersion.CSharp13)]
    [Arguments(LanguageVersion.CSharp14)]
    public async Task LanguageVersion_UsesCompatiblePropertyStorageAsync(LanguageVersion languageVersion)
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run() { }

                                  public void UseGeneratedMembers() => RunEvent.Raise();
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source, languageVersion: languageVersion);

        await Verify(driver.GetRunResult()).UseParameters(languageVersion);
    }

    [Test]
    public async Task UnannotatedOverloads_PreserveDelegateBindingAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run(Autodesk.Revit.UI.UIApplication application) { }

                                  private void Run() { }
                                  private void Run(string label) { }

                                  public void UseGeneratedMembers() => RunEvent.Raise();
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Verify(driver.GetRunResult());
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task MissingToolkitAttribute_GeneratesNothingAsync(bool hasUnrelatedAttribute)
    {
        var source = $$"""
                       namespace Consumer;

                       public sealed class ExternalEventAttribute : System.Attribute { }

                       public partial class Model
                       {
                           {{(hasUnrelatedAttribute ? "[ExternalEvent]" : string.Empty)}}
                           private void Run() { }
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
    }

    [Test]
    [Arguments("class Model", "")]
    [Arguments("class Outer { public partial class Model", "}")]
    public async Task NonPartialContainingType_GeneratesNothingAsync(string declaration, string closingDeclaration)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public {{declaration}}
                       {
                           [ExternalEvent]
                           private void Run() { }
                       }
                       {{closingDeclaration}}
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
    }

    [Test]
    public async Task PartialTypeAcrossFiles_GeneratesMembersInSameTypeAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run(int quantity) { }
                              }
                              """;

        const string consumer = """
                                namespace Consumer;

                                public partial class Model
                                {
                                    public void UseGeneratedMembers() => RunEvent.Raise(42);
                                }
                                """;

        var (driver, _) = await GeneratorTest.RunAsync(source, additionalSources: [consumer]);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult());
    }

    [Test]
    public async Task DistinctAnnotatedMethods_GenerateIndependentEventsAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run() { }

                                  [ExternalEvent]
                                  private int Count(string label) => label.Length;

                                  public void UseGeneratedMembers()
                                  {
                                      _ = RunEvent.Raise();
                                      _ = RunAsyncEvent.RaiseAsync();
                                      _ = CountAsyncEvent.RaiseAsync("Walls");
                                  }
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(2);
        await Verify(driver.GetRunResult());
    }

    [Test]
    public async Task UIApplicationAfterFirstParameter_RemainsUserArgumentAsync()
    {
        const string source = """
                              using Autodesk.Revit.UI;
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run(int quantity, UIApplication application) { }

                                  public void UseGeneratedMembers(UIApplication application)
                                  {
                                      _ = RunEvent.Raise(42, application);
                                      _ = RunAsyncEvent.RaiseAsync(42, application);
                                  }
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Verify(driver.GetRunResult());
    }

    [Test]
    public async Task UnrelatedEdit_ReusesGeneratedOutputAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run(int quantity) { }

                                  public void UseGeneratedMembers() => RunEvent.Raise(42);
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);
        var previous = driver.GetRunResult();
        var unrelated = CSharpSyntaxTree.ParseText("namespace Consumer; public class Unrelated { }", (CSharpParseOptions)input.SyntaxTrees.First().Options);
        var updated = driver.RunGeneratorsAndUpdateCompilation(input.AddSyntaxTrees(unrelated), out var output, out var diagnostics);
        var current = updated.GetRunResult();
        var reasons = current.Results.Single().TrackedOutputSteps.Values
            .SelectMany(static steps => steps)
            .SelectMany(static step => step.Outputs)
            .Select(static result => result.Reason)
            .ToArray();

        await GeneratorTest.AssertCompilesAsync(output);
        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(current.GeneratedTrees.Select(static tree => tree.ToString())).IsEquivalentTo(previous.GeneratedTrees.Select(static tree => tree.ToString()));
        await Assert.That(reasons).IsNotEmpty();
        await Assert.That(reasons.All(static reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)).IsTrue();
    }
}
