using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

[Category("Regression")]
public sealed class ExtensionContainerTests
{
    [Test]
    [Arguments("public static class ModelExtensions { }")]
    [Arguments("public partial class ModelExtensions { }")]
    [Arguments("namespace ModelExtensions { }")]
    [Arguments("internal static partial class ModelExtensions { }")]
    public async Task IncompatibleExtensionContainer_ReportsDiagnosticAsync(string declaration)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       namespace Consumer
                       {
                           {{declaration}}
                           public partial class Model
                           {
                               [ExternalEvent] private void Run(int first, int second) { }
                           }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source, ["RVTTK0007"]);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Single().GetMessage()).Contains("member 'ModelExtensions'");
    }

    [Test]
    [Arguments("public", "public")]
    [Arguments("internal", "internal")]
    [Arguments("internal", "public")]
    public async Task CompatiblePartialContainer_PreservesAccessibilityAsync(string ownerAccessibility, string containerAccessibility)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       namespace Consumer
                       {
                           {{containerAccessibility}} static partial class ModelExtensions { }
                           {{ownerAccessibility}} partial class Model
                           {
                               [ExternalEvent] private void Run(int first, int second) { }
                           }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        var output = input.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees);
        var generatedContainer = output.GetTypeByMetadataName("Consumer.ModelExtensions")!;
        await Assert.That(generatedContainer.DeclaredAccessibility).IsEqualTo(containerAccessibility == "public" ? Accessibility.Public : Accessibility.Internal);
        await Assert.That(generatedContainer.GetMembers("Raise")).Count().IsEqualTo(1);
        await Assert.That(generatedContainer.GetMembers("RaiseAsync")).Count().IsEqualTo(1);
    }

    [Test]
    public async Task FileLocalContainerName_DoesNotConflictAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              file class ModelExtensions { }
                              public partial class Model
                              {
                                  [ExternalEvent] private void Run(int first, int second) { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        var output = input.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees);
        await Assert.That(output.GetTypeByMetadataName("ModelExtensions")!.IsStatic).IsTrue();
    }

    [Test]
    public async Task GenericContainerName_DoesNotConflictAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public class ModelExtensions<T> { }
                              public partial class Model
                              {
                                  [ExternalEvent] private void Run(int first, int second) { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        var output = input.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees);
        await Assert.That(output.GetTypeByMetadataName("ModelExtensions")!.IsStatic).IsTrue();
        await Assert.That(output.GetTypeByMetadataName("ModelExtensions`1")!.IsStatic).IsFalse();
    }

    [Test]
    public async Task PrivateNestedType_DoesNotReserveExtensionContainerAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public class OuterExtensions { }
                              public partial class Outer
                              {
                                  private partial class Model
                                  {
                                      [ExternalEvent] private void Run(int first, int second) { }
                                  }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        var output = input.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees);
        await Assert.That(output.GetTypeByMetadataName("OuterExtensions")!.GetMembers("Raise")).IsEmpty();
    }

    [Test]
    public async Task SingleUserArgument_DoesNotReserveExtensionContainerAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public class ModelExtensions { }
                              public partial class Model
                              {
                                  [ExternalEvent] private void Run(Autodesk.Revit.UI.UIApplication? application, int value) { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        var output = input.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees);
        await Assert.That(output.GetTypeByMetadataName("ModelExtensions")!.GetMembers("Raise")).IsEmpty();
    }
}
