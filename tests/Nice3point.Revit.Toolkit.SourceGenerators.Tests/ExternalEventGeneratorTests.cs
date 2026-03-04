using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.Analyzers.Diagnostics;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

public sealed class ExternalEventGeneratorTests
{
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).Count().IsEqualTo(1);

        var output = generated[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("DoWorkEvent");
            await Assert.That(output).Contains("DoWorkAsyncEvent");
            await Assert.That(output).Contains("ExternalEvent");
            await Assert.That(output).Contains("AsyncExternalEvent");
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).Count().IsEqualTo(1);

        var output = generated[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("DoWorkEvent");
            await Assert.That(output).Contains("DoWorkAsyncEvent");
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).Count().IsEqualTo(1);

        var output = generated[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).DoesNotContain("CalculateEvent ");
            await Assert.That(output).Contains("CalculateAsyncEvent");
            await Assert.That(output).Contains("AsyncRequestExternalEvent<int>");
        }
    }

    [Test]
    public async Task MethodWithExtraParams_GeneratesBuiltInGenericProperty()
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).Count().IsEqualTo(1);

        var output = generated[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("IExternalEvent<string>");
            await Assert.That(output).Contains("ExternalEvent<string>");
            await Assert.That(output).Contains("DoWorkEvent");
            await Assert.That(output).Contains("IAsyncExternalEvent<string>");
            await Assert.That(output).Contains("AsyncExternalEvent<string>");
            await Assert.That(output).Contains("DoWorkAsyncEvent");
            await Assert.That(output).Contains("[global::System.CodeDom.Compiler.GeneratedCode(");
            await Assert.That(output).Contains("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        }
    }

    [Test]
    public async Task MethodWithMultipleExtraParams_GeneratesRecordAndProperty()
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).Count().IsEqualTo(1);

        var output = generated[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("sealed record DoWorkArgs(string Title, int Count)");
            await Assert.That(output).Contains("IExternalEvent<DoWorkArgs>");
            await Assert.That(output).Contains("ExternalEvent<DoWorkArgs>");
            await Assert.That(output).Contains("IAsyncExternalEvent<DoWorkArgs>");
            await Assert.That(output).Contains("AsyncExternalEvent<DoWorkArgs>");
            await Assert.That(output).Contains("args.Title, args.Count");
            await Assert.That(output).Contains("DoWorkEvent");
            await Assert.That(output).Contains("DoWorkAsyncEvent");
        }
    }

    [Test]
    public async Task MethodWithMultipleExtraParams_GeneratesExtensionMethod()
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).Count().IsEqualTo(1);

        var output = generated[0];
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
    public async Task ReturningMethodWithMultipleExtraParams_GeneratesAsyncExtensionMethod()
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).Count().IsEqualTo(1);

        var output = generated[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("public static partial class MyViewModelExtensions");
            await Assert.That(output).Contains("RaiseAsync(this");
            await Assert.That(output).Contains("IAsyncRequestExternalEvent<MyViewModel.CalculateArgs, int>");
            await Assert.That(output).Contains("return externalEvent.RaiseAsync(new MyViewModel.CalculateArgs(title, count));");
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).Count().IsEqualTo(1);

        var output = generated[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("ExternalEventOptions");
            await Assert.That(output).Contains("AllowDirectInvocation");
        }
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).Count().IsEqualTo(1);

        var output = generated[0];
        using (Assert.Multiple())
        {
            await Assert.That(output).Contains("static");
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

        var (diagnostics, _) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Id == DiagnosticDescriptors.TaskReturnNotSupported.Id)).IsNotEmpty();
    }

    [Test]
    public async Task NonPartialType_GeneratesNothing()
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

        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(generated).IsEmpty();
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

        var (diagnostics, _) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Id == DiagnosticDescriptors.MethodIsGeneric.Id)).IsNotEmpty();
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

        var (diagnostics, _) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Id == DiagnosticDescriptors.DuplicateMethodOverload.Id)).IsNotEmpty();
    }

    [Test]
    public async Task AsyncVoidMethod_GeneratesCode()
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

        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(generated).IsNotEmpty();
    }

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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(generated).IsEmpty();
    }
}