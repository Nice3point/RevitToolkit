using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.Analyzers.Diagnostics;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

public sealed class ExternalEventGeneratorTests
{
    [Test]
    public async Task NoAttribute_GeneratesNothing()
    {
        const string source = """
                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generatedSources).IsEmpty();
    }

    [Test]
    public async Task PublicClassNoExtraParams_NoExtensionClassGenerated()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).DoesNotContain("Extensions");
    }

    [Test]
    public async Task SimpleVoidMethod_GeneratesSyncAndAsyncProperties()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(generatedSources).Count().IsEqualTo(1);

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("IExternalEvent DoWorkEvent");
            await Assert.That(output).Contains("IAsyncExternalEvent DoWorkAsyncEvent");
            await Assert.That(output).Contains("new global::Nice3point.Revit.Toolkit.External.ExternalEvent(new global::System.Action(DoWork)");
            await Assert.That(output).Contains("new global::Nice3point.Revit.Toolkit.External.AsyncExternalEvent(new global::System.Action(DoWork)");
            await Assert.That(output).Contains("[global::System.CodeDom.Compiler.GeneratedCode(");
            await Assert.That(output).Contains("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        }
    }

    [Test]
    public async Task VoidMethodWithUIApplication_GeneratesSyncAndAsyncProperties()
    {
        const string source = """
                              using Autodesk.Revit.UI;
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork(UIApplication app) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(generatedSources).Count().IsEqualTo(1);

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("IExternalEvent DoWorkEvent");
            await Assert.That(output).Contains("IAsyncExternalEvent DoWorkAsyncEvent");
            await Assert.That(output).Contains("new global::System.Action<global::Autodesk.Revit.UI.UIApplication>(DoWork)");
        }
    }

    [Test]
    public async Task AsyncVoidMethod_GeneratesCodeWithoutErrors()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private async void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        using (Assert.Multiple())
        {
            await Assert.That(generatedSources).IsNotEmpty();
            await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        }
    }

    [Test]
    public async Task ReturningMethod_GeneratesOnlyAsyncProperty()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private int Calculate() => 42;
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(generatedSources).Count().IsEqualTo(1);

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).DoesNotContain("IExternalEvent CalculateEvent");
            await Assert.That(output).Contains("IAsyncRequestExternalEvent<int> CalculateAsyncEvent");
            await Assert.That(output).Contains("new global::Nice3point.Revit.Toolkit.External.AsyncRequestExternalEvent<int>(new global::System.Func<int>(Calculate)");
        }
    }

    [Test]
    public async Task ReturningMethodWithUIApplication_GeneratesExplicitFuncDelegate()
    {
        const string source = """
                              using Autodesk.Revit.UI;
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private int Calculate(UIApplication app) => 42;
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("IAsyncRequestExternalEvent<int> CalculateAsyncEvent");
            await Assert.That(output).Contains("new global::System.Func<global::Autodesk.Revit.UI.UIApplication, int>(Calculate)");
        }
    }

    [Test]
    public async Task ReturningMethodWithSingleExtraParam_GeneratesGenericAsyncProperty()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private int Calculate(string input) => 42;
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("IAsyncRequestExternalEvent<string, int> CalculateAsyncEvent");
            await Assert.That(output).Contains("new global::System.Func<string, int>(Calculate)");
        }
    }

    [Test]
    public async Task ReturningMethodWithMultipleExtraParams_GeneratesRecordAndAsyncOnly()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private int Calculate(string title, int count) => 42;
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("sealed record CalculateArgs(string Title, int Count)");
            await Assert.That(output).Contains("IAsyncRequestExternalEvent<CalculateArgs, int> CalculateAsyncEvent");
            await Assert.That(output).DoesNotContain("IExternalEvent<CalculateArgs> CalculateEvent");
            await Assert.That(output).DoesNotContain("IAsyncExternalEvent<CalculateArgs> CalculateAsyncEvent");
        }
    }

    [Test]
    public async Task ReturningMethodWithMultipleExtraParamsAndUIApplication_GeneratesExtension()
    {
        const string source = """
                              using Autodesk.Revit.UI;
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private int Calculate(UIApplication app, string title, int count) => 42;
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("public static partial class MyViewModelExtensions");
            await Assert.That(output).Contains("RaiseAsync(this");
            await Assert.That(output).Contains("IAsyncRequestExternalEvent<MyViewModel.CalculateArgs, int>");
            await Assert.That(output).Contains("return externalEvent.RaiseAsync(new MyViewModel.CalculateArgs(title, count));");
            await Assert.That(output).Contains("sealed record CalculateArgs(string Title, int Count)");
        }
    }

    [Test]
    public async Task SingleExtraParam_GeneratesGenericProperty()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork(string message) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(generatedSources).Count().IsEqualTo(1);

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("IExternalEvent<string> DoWorkEvent");
            await Assert.That(output).Contains("new global::Nice3point.Revit.Toolkit.External.ExternalEvent<string>(new global::System.Action<string>(DoWork)");
            await Assert.That(output).Contains("IAsyncExternalEvent<string> DoWorkAsyncEvent");
            await Assert.That(output).Contains("new global::Nice3point.Revit.Toolkit.External.AsyncExternalEvent<string>(new global::System.Action<string>(DoWork)");
        }
    }

    [Test]
    public async Task SingleExtraParamWithUIApplication_GeneratesGenericProperty()
    {
        const string source = """
                              using Autodesk.Revit.UI;
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork(UIApplication app, string message) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("IExternalEvent<string> DoWorkEvent");
            await Assert.That(output).Contains("IAsyncExternalEvent<string> DoWorkAsyncEvent");
        }
    }

    [Test]
    public async Task NullableExtraParam_PreservesNullabilityAnnotation()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork(string? message) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).Contains("IExternalEvent<string?> DoWorkEvent");
    }

    [Test]
    public async Task MultipleExtraParams_GeneratesRecordAndProperties()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork(string title, int count) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(generatedSources).Count().IsEqualTo(1);

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("sealed record DoWorkArgs(string Title, int Count)");
            await Assert.That(output).Contains("IExternalEvent<DoWorkArgs> DoWorkEvent");
            await Assert.That(output).Contains("IAsyncExternalEvent<DoWorkArgs> DoWorkAsyncEvent");
            await Assert.That(output).Contains("args.Title, args.Count");
        }
    }

    [Test]
    public async Task MultipleExtraParamsWithoutUIApplication_GeneratesLambdaWithoutApplication()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork(string title, int count) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("(args) => DoWork(args.Title, args.Count)");
            await Assert.That(output).DoesNotContain("application, args");
        }
    }

    [Test]
    public async Task MultipleExtraParamsWithUIApplication_GeneratesLambdaWithApplication()
    {
        const string source = """
                              using Autodesk.Revit.UI;
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork(UIApplication app, string title, int count) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).Contains("(application, args) => DoWork(application, args.Title, args.Count)");
    }

    [Test]
    public async Task MultipleExtraParams_GeneratesExtensionMethods()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork(string title, int count) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("public static partial class MyViewModelExtensions");
            await Assert.That(output).Contains("this global::Nice3point.Revit.Toolkit.External.IExternalEvent<MyViewModel.DoWorkArgs> externalEvent");
            await Assert.That(output).Contains("this global::Nice3point.Revit.Toolkit.External.IAsyncExternalEvent<MyViewModel.DoWorkArgs> externalEvent");
            await Assert.That(output).Contains("string title, int count");
            await Assert.That(output).Contains("return externalEvent.Raise(new MyViewModel.DoWorkArgs(title, count));");
            await Assert.That(output).Contains("return externalEvent.RaiseAsync(new MyViewModel.DoWorkArgs(title, count));");
        }
    }

    [Test]
    public async Task InternalClass_GeneratesInternalExtensionClass()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              internal partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork(string title, int count) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("internal static partial class MyViewModelExtensions");
            await Assert.That(output).DoesNotContain("public static partial class MyViewModelExtensions");
        }
    }

    [Test]
    public async Task InternalNestedInPublic_GeneratesInternalExtensionClass()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class Outer
                              {
                                  internal partial class Inner
                                  {
                                      [ExternalEvent]
                                      private void DoWork(string title, int count) { }
                                  }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).Contains("internal static partial class OuterExtensions");
    }

    [Test]
    public async Task PublicClass_GeneratesPublicAccessibility()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).Contains("public partial class MyViewModel");
    }

    [Test]
    public async Task InternalClass_GeneratesInternalAccessibility()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              internal partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).Contains("internal partial class MyViewModel");
    }

    [Test]
    public async Task StaticMethod_GeneratesStaticProperties()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private static void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("public static global::Nice3point.Revit.Toolkit.External.IExternalEvent DoWorkEvent");
            await Assert.That(output).Contains("public static global::Nice3point.Revit.Toolkit.External.IAsyncExternalEvent DoWorkAsyncEvent");
        }
    }

    [Test]
    public async Task StaticMethodWithExtraParam_GeneratesStaticGenericProperties()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private static void DoWork(string message) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("public static global::Nice3point.Revit.Toolkit.External.IExternalEvent<string> DoWorkEvent");
            await Assert.That(output).Contains("public static global::Nice3point.Revit.Toolkit.External.IAsyncExternalEvent<string> DoWorkAsyncEvent");
        }
    }

    [Test]
    public async Task StaticContainingClass_GeneratesStaticPartialClass()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public static partial class MyHelpers
                              {
                                  [ExternalEvent]
                                  private static void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).Contains("public static partial class MyHelpers");
    }

    [Test]
    public async Task NestedTypes_GeneratesNestedPartialHierarchy()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class Outer
                              {
                                  public partial class Inner
                                  {
                                      [ExternalEvent]
                                      private void DoWork() { }
                                  }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(generatedSources).Count().IsEqualTo(1);

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("public partial class Outer");
            await Assert.That(output).Contains("public partial class Inner");
            await Assert.That(output).Contains("IExternalEvent DoWorkEvent");
        }
    }

    [Test]
    public async Task StructContainingType_GeneratesWithStructKeyword()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial struct MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("public partial struct MyViewModel");
            await Assert.That(output).Contains("IExternalEvent DoWorkEvent");
        }
    }

    [Test]
    public async Task RecordStructContainingType_GeneratesCorrectKeyword()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial record struct MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).Contains("public partial record struct MyViewModel");
    }

    [Test]
    public async Task RecordClassContainingType_GeneratesCorrectKeyword()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial record class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).Contains("public partial record class MyViewModel");
    }

    [Test]
    public async Task GlobalNamespace_GeneratesCodeWithoutNamespaceBlock()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(generatedSources).Count().IsEqualTo(1);

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).DoesNotContain("namespace");
            await Assert.That(output).Contains("IExternalEvent DoWorkEvent");
            await Assert.That(output).Contains("IAsyncExternalEvent DoWorkAsyncEvent");
        }
    }

    [Test]
    public async Task AllowDirectInvocation_GeneratesOptionsCode()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent(AllowDirectInvocation = true)]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        await Assert.That(output).Contains("ExternalEventOptions.AllowDirectInvocation");
    }

    [Test]
    public async Task AllowDirectInvocationWithMultipleParams_GeneratesOptionsWithRecord()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent(AllowDirectInvocation = true)]
                                  private void DoWork(string title, int count) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics).IsEmpty();

        var output = generatedSources[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("ExternalEventOptions.AllowDirectInvocation");
            await Assert.That(output).Contains("sealed record DoWorkArgs");
        }
    }

    [Test]
    public async Task NonPartialType_ReportsError()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        using (Assert.Multiple())
        {
            await Assert.That(diagnostics.Where(d => d.Id == DiagnosticDescriptors.ExternalEventContainingTypeNotPartial.Id)).IsNotEmpty();
            await Assert.That(generatedSources).IsEmpty();
        }
    }

    [Test]
    public async Task NonPartialNestedOuterType_ReportsError()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public class Outer
                              {
                                  public partial class Inner
                                  {
                                      [ExternalEvent]
                                      private void DoWork() { }
                                  }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        using (Assert.Multiple())
        {
            await Assert.That(diagnostics.Where(d => d.Id == DiagnosticDescriptors.ExternalEventContainingTypeNotPartial.Id)).IsNotEmpty();
            await Assert.That(generatedSources).IsEmpty();
        }
    }

    [Test]
    public async Task GenericMethod_ReportsError()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork<T>() { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        using (Assert.Multiple())
        {
            await Assert.That(diagnostics.Where(d => d.Id == DiagnosticDescriptors.ExternalEventGenericMethod.Id)).IsNotEmpty();
            await Assert.That(generatedSources).IsEmpty();
        }
    }

    [Test]
    public async Task TaskReturn_ReportsError()
    {
        const string source = """
                              using System.Threading.Tasks;
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private Task DoWorkAsync() => Task.CompletedTask;
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        using (Assert.Multiple())
        {
            await Assert.That(diagnostics.Where(d => d.Id == DiagnosticDescriptors.ExternalEventTaskReturnNotSupported.Id)).IsNotEmpty();
            await Assert.That(generatedSources).IsEmpty();
        }
    }

    [Test]
    public async Task TaskGenericReturn_ReportsError()
    {
        const string source = """
                              using System.Threading.Tasks;
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private Task<int> CalculateAsync() => Task.FromResult(42);
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        using (Assert.Multiple())
        {
            await Assert.That(diagnostics.Where(d => d.Id == DiagnosticDescriptors.ExternalEventTaskReturnNotSupported.Id)).IsNotEmpty();
            await Assert.That(generatedSources).IsEmpty();
        }
    }

    [Test]
    public async Task DuplicateOverloads_ReportsError()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace TestApplication;

                              public partial class MyViewModel
                              {
                                  [ExternalEvent]
                                  private void DoWork() { }

                                  [ExternalEvent]
                                  private void DoWork(string message) { }
                              }
                              """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        using (Assert.Multiple())
        {
            await Assert.That(diagnostics.Where(d => d.Id == DiagnosticDescriptors.ExternalEventDuplicateMethodOverload.Id)).IsNotEmpty();
            await Assert.That(generatedSources).IsEmpty();
        }
    }
}
