using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

[Category("Regression")]
public sealed class UnsupportedSignatureTests
{
    [Test]
    public async Task InaccessibleContainingTypeArgument_ReportsDiagnosticAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public class Container<T>
                              {
                                  public sealed class Nested { }
                              }
                              public partial class Model
                              {
                                  private sealed class Hidden { }
                                  [ExternalEvent] private void Run(Container<Hidden>.Nested value) { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source, ["RVTTK0006"]);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Single().GetMessage()).Contains("at least as accessible");
    }

    [Test]
    public async Task NestedPrivateType_CanExposeEnclosingPrivatePayloadAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public partial class Outer
                              {
                                  private sealed class Payload { }
                                  public partial class Container
                                  {
                                      private partial class Model
                                      {
                                          [ExternalEvent] private Payload Run(Payload value) => value;
                                      }
                                  }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
    }

    [Test]
    [Arguments("int*")]
    [Arguments("delegate*<void>")]
    public async Task UnsafeSignature_ReportsDiagnosticAsync(string parameterType)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       public partial class Model
                       {
                           [ExternalEvent] private unsafe void Run({{parameterType}} value) { }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source, ["RVTTK0006"], allowUnsafe: true);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Single().GetMessage()).Contains("generic type arguments");
    }

    [Test]
    public async Task ExplicitInterfaceImplementation_ReportsDiagnosticAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public interface IModel { void Run(); }
                              public partial class Model : IModel
                              {
                                  [ExternalEvent] void IModel.Run() { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source, ["RVTTK0006"]);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Single().GetMessage()).Contains("explicit interface implementations");
    }

    [Test]
    public async Task UnimplementedPartialMethod_ReportsDiagnosticAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public partial class Model
                              {
                                  [ExternalEvent] partial void Run();
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source, ["RVTTK0006"]);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Single().GetMessage()).Contains("partial methods require an implementation");
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ImplementedPartialMethod_SupportsEitherAttributeLocationAsync(bool annotateDefinition)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       public partial class Model
                       {
                           {{(annotateDefinition ? "[ExternalEvent]" : string.Empty)}}
                           partial void Run();
                           {{(annotateDefinition ? string.Empty : "[ExternalEvent]")}}
                           partial void Run() { }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
    }

    [Test]
    [Arguments("public partial class Model", "public int RunEvent;", "RunEvent")]
    [Arguments("public partial class Model", "public int RunAsyncEvent;", "RunAsyncEvent")]
    [Arguments("public partial class Model", "public sealed class RunArgs { }", "RunArgs")]
    [Arguments("public partial class RunEvent", "", "RunEvent")]
    [Arguments("public partial class Model<RunEvent>", "", "RunEvent")]
    public async Task GeneratedMemberCollision_ReportsDiagnosticAsync(string declaration, string member, string conflictingName)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       {{declaration}}
                       {
                           {{member}}
                           [ExternalEvent]
                           private void Run(int first, int second) { }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source, ["RVTTK0007"]);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Single().GetMessage()).Contains($"member '{conflictingName}'");
    }

    [Test]
    public async Task ValueReturningMethod_DoesNotReserveUnusedSyncPropertyAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public partial class Model
                              {
                                  public int RunEvent;
                                  [ExternalEvent] private int Run() => 42;
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
    }

    [Test]
    [Arguments("private")]
    [Arguments("protected")]
    [Arguments("private protected")]
    [Arguments("internal")]
    public async Task EquallyAccessibleSiblingType_RemainsSupportedAsync(string accessibility)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       public partial class Outer
                       {
                           {{accessibility}} sealed class Payload { }
                           {{accessibility}} partial class Model
                           {
                               [ExternalEvent] private Payload Run(Payload value) => value;
                           }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
    }

    [Test]
    public async Task SiblingGeneratedMemberCollision_ReportsBothMethodsAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public partial class Model
                              {
                                  [ExternalEvent] private void Run() { }
                                  [ExternalEvent] private void RunAsync() { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source, ["RVTTK0007", "RVTTK0007"]);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Select(static diagnostic => diagnostic.GetMessage()))
            .IsEquivalentTo([
                "Method 'Run' marked with [ExternalEvent] generates member 'RunAsyncEvent', which conflicts with another declaration",
                "Method 'RunAsync' marked with [ExternalEvent] generates member 'RunAsyncEvent', which conflicts with another declaration"
            ]);
    }

    [Test]
    [Arguments("public partial class Model", "private void Run(ref int value) { }", "passed by value")]
    [Arguments("public partial class Model", "private void Run(out int value) => value = 0;", "passed by value")]
    [Arguments("public partial class Model", "private void Run(in int value) { }", "passed by value")]
    [Arguments("public partial class Model", "private ref int Run() => throw new System.Exception();", "passed by value")]
    [Arguments("public partial class Model", "private ref readonly int Run() => throw new System.Exception();", "passed by value")]
    [Arguments("public partial class Model", "private void Run(System.Span<int> value) { }", "generic type arguments")]
    [Arguments("public partial class Model", "private System.Span<int> Run() => default;", "generic type arguments")]
    [Arguments("public readonly partial struct Model", "private void Run() { }", "mutable containing structs")]
    [Arguments("public ref partial struct Model", "private void Run() { }", "non-ref structs")]
    [Arguments("file partial class Model", "private void Run() { }", "non-file-local")]
    [Arguments("public partial interface Model", "private void Run() { }", "non-ref structs")]
    public async Task UnsupportedShape_ReportsDiagnosticWithoutGeneratingSourceAsync(string declaration, string method, string reason)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       {{declaration}}
                       {
                           [ExternalEvent]
                           {{method}}
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source, ["RVTTK0006"]);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        var diagnostic = driver.GetRunResult().Diagnostics.Single();
        await Assert.That(diagnostic.Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostic.GetMessage()).Contains(reason);
        await Assert.That(diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan)).IsEqualTo("Run");
    }

    [Test]
    [Arguments("private sealed class Payload { }", "Payload value")]
    [Arguments("internal sealed class Payload { }", "Payload value")]
    [Arguments("private sealed class Payload { }", "System.Collections.Generic.List<Payload> value")]
    public async Task InaccessibleSignatureType_ReportsDiagnosticAsync(string payload, string parameter)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       public partial class Model
                       {
                           {{payload}}
                           [ExternalEvent]
                           private void Run({{parameter}}) { }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source, ["RVTTK0006"]);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Single().GetMessage()).Contains("at least as accessible");
    }

    [Test]
    [Arguments(16)]
    [Arguments(17)]
    public async Task DelegateArity_ValidatesSupportedBoundaryAsync(int parameterCount)
    {
        var parameters = string.Join(", ", Enumerable.Range(0, parameterCount).Select(static index => $"int value{index}"));
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       public partial class Model
                       {
                           [ExternalEvent]
                           private void Run({{parameters}}) { }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source, parameterCount == 16 ? [] : ["RVTTK0006"]);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(parameterCount == 16 ? 1 : 0);
    }

    [Test]
    public async Task ReadonlyStruct_StaticMethodRemainsSupportedAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public readonly partial struct Model
                              {
                                  [ExternalEvent]
                                  private static void Run() { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
    }
}
