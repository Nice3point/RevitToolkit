using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.SourceGenerators.Diagnostics;

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
        await Assert.That(output).Contains("DoWorkEvent");
        await Assert.That(output).Contains("DoWorkAsyncEvent");
        await Assert.That(output).Contains("ExternalEvent");
        await Assert.That(output).Contains("AsyncExternalEvent");
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
        await Assert.That(output).Contains("DoWorkEvent");
        await Assert.That(output).Contains("DoWorkAsyncEvent");
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
        await Assert.That(output).DoesNotContain("CalculateEvent ");
        await Assert.That(output).Contains("CalculateAsyncEvent");
        await Assert.That(output).Contains("AsyncExternalEvent<int>");
    }

    [Test]
    public async Task MethodWithExtraParams_GeneratesCustomClasses()
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
        await Assert.That(output).Contains("DoWorkExternalEvent");
        await Assert.That(output).Contains("DoWorkAsyncExternalEvent");
        await Assert.That(output).Contains("sealed class DoWorkExternalEvent");
        await Assert.That(output).Contains("sealed class DoWorkAsyncExternalEvent");
        await Assert.That(output).Contains("[global::System.CodeDom.Compiler.GeneratedCode(");
        await Assert.That(output).Contains("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
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
        await Assert.That(output).Contains("ExternalEventOptions");
        await Assert.That(output).Contains("AllowDirectInvocation");
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
        await Assert.That(output).Contains("static");
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

        var (diagnostics, _) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Id == DiagnosticDescriptors.ContainingTypeNotPartial.Id)).IsNotEmpty();
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
    public async Task AsyncVoidMethod_ReportsWarning()
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

        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        await Assert.That(diagnostics.Where(diagnostic => diagnostic.Id == DiagnosticDescriptors.MethodIsAsyncVoid.Id)).IsNotEmpty();
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