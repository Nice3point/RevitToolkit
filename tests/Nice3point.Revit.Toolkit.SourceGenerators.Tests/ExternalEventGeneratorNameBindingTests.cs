using Microsoft.CodeAnalysis.CSharp;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

[Category("Regression")]
[NotInParallel("GeneratorSnapshots")]
public sealed class ExternalEventGeneratorNameBindingTests
{
    [Test]
    [Arguments("args", "class", false)]
    [Arguments("application", "class", true)]
    [Arguments("handler", "struct", false)]
    public async Task MethodName_MatchesGeneratedLocalNameWithoutShadowingAsync(string methodName, string typeKind, bool hasApplication)
    {
        var parameters = hasApplication ? "Autodesk.Revit.UI.UIApplication uiApplication, int first, int second" : "int first, int second";
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial {{typeKind}} Model
                       {
                           [ExternalEvent]
                           private void {{methodName}}({{parameters}}) { }

                           public void UseGeneratedMembers()
                           {
                               _ = {{methodName}}Event.Raise(first: 2, second: 5);
                               _ = {{methodName}}AsyncEvent.RaiseAsync(first: 2, second: 5);
                           }
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(1);
        await Verify(driver.GetRunResult()).UseParameters(methodName);
    }

    [Test]
    [Arguments("clone")]
    [Arguments("equals")]
    [Arguments("getHashCode")]
    [Arguments("toString")]
    [Arguments("equalityContract")]
    [Arguments("deconstruct")]
    [Arguments("printMembers")]
    public async Task RecordArgumentName_DoesNotCollideWithSynthesizedMembersAsync(string parameterName)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial class Model
                       {
                           [ExternalEvent]
                           private void Run(int {{parameterName}}, int count) { }

                           public void UseGeneratedMembers()
                           {
                               _ = RunEvent.Raise({{parameterName}}: 2, count: 5);
                               _ = RunAsyncEvent.RaiseAsync({{parameterName}}: 2, count: 5);
                           }
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(1);
        await Verify(driver.GetRunResult()).UseParameters(parameterName);
    }

    [Test]
    public async Task ShadowedGenericParameter_PreservesDistinctExtensionTypeArgumentsAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Outer<TValue> where TValue : class
                              {
                                  public partial class Inner<TValue> where TValue : struct
                                  {
                                      [ExternalEvent]
                                      private void Run(TValue value, int count) { }
                                  }
                              }

                              public static class Usage
                              {
                                  public static void UseGeneratedMembers()
                                  {
                                      var model = new Outer<string>.Inner<int>();
                                      _ = model.RunEvent.Raise(value: 2, count: 5);
                                      _ = model.RunAsyncEvent.RaiseAsync(value: 2, count: 5);
                                  }
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(1);
        await Verify(driver.GetRunResult());
    }

    [Test]
    [Arguments("public", "internal")]
    [Arguments("internal", "public")]
    public async Task DifferentGenericArities_PreserveIndependentExtensionAccessibilityAsync(string ordinaryAccessibility, string genericAccessibility)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       {{ordinaryAccessibility}} partial class Model
                       {
                           [ExternalEvent]
                           private void Run(int value, int count) { }
                       }

                       {{genericAccessibility}} partial class Model<TValue>
                       {
                           [ExternalEvent]
                           private void Run(TValue value, int count) { }
                       }

                       internal static class Usage
                       {
                           public static void UseGeneratedMembers()
                           {
                               _ = new Model().RunEvent.Raise(value: 2, count: 5);
                               _ = new Model<int>().RunEvent.Raise(value: 2, count: 5);
                               _ = new Model().RunAsyncEvent.RaiseAsync(value: 2, count: 5);
                               _ = new Model<int>().RunAsyncEvent.RaiseAsync(value: 2, count: 5);
                           }
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(2);
        await Verify(driver.GetRunResult()).UseParameters(ordinaryAccessibility, genericAccessibility);
    }

    [Test]
    [Arguments(LanguageVersion.CSharp13)]
    [Arguments(LanguageVersion.CSharp14)]
    public async Task CaseSensitiveMethodNames_KeepIndependentBackingFieldsAsync(LanguageVersion languageVersion)
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model
                              {
                                  [ExternalEvent]
                                  private void Run() { }

                                  [ExternalEvent]
                                  private void run() { }

                                  public void UseGeneratedMembers()
                                  {
                                      _ = RunEvent.Raise();
                                      _ = runEvent.Raise();
                                      _ = RunAsyncEvent.RaiseAsync();
                                      _ = runAsyncEvent.RaiseAsync();
                                  }
                              }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source, languageVersion: languageVersion);

        await Assert.That(driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(2);
        await Verify(driver.GetRunResult()).UseParameters(languageVersion);
    }

    [Test]
    [Arguments("field", "Model")]
    [Arguments("Consumer", "field")]
    public async Task FieldContextualKeyword_PreservesNamespaceAndTypeBindingAsync(string namespaceName, string typeName)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       namespace {{namespaceName}};

                       public partial class {{typeName}}
                       {
                           [ExternalEvent]
                           private void Run(int value, int count) { }

                           public void UseGeneratedMembers() => RunEvent.Raise(value: 2, count: 5);
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);

        await Assert.That(driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(1);
        await Verify(driver.GetRunResult()).UseParameters(namespaceName, typeName);
    }
}
