using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

public sealed class GeneratorIncrementalityTests
{
    [Test]
    public async Task FrameworkReferenceChange_UpdatesLockTypeAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public partial class Model
                              {
                                  [ExternalEvent] private void Run() { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);
        var cancellationToken = TestContext.Current!.Execution.CancellationToken;
        var references = await ReferenceAssemblies.Net.Net90.ResolveAsync(LanguageNames.CSharp, cancellationToken);
        var updated = driver.RunGeneratorsAndUpdateCompilation(input.WithReferences(references), out var output, out var diagnostics, cancellationToken);
        var gate = output.GetTypeByMetadataName("Model")!.GetMembers("_RunEventLock").OfType<IFieldSymbol>().Single();

        await GeneratorTest.AssertCompilesAsync(output);
        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(gate.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).IsEqualTo("global::System.Threading.Lock");

        var reverted = updated.RunGeneratorsAndUpdateCompilation(input, out var revertedOutput, out var revertedDiagnostics, cancellationToken);
        var revertedGate = revertedOutput.GetTypeByMetadataName("Model")!.GetMembers("_RunEventLock").OfType<IFieldSymbol>().Single();

        await GeneratorTest.AssertCompilesAsync(revertedOutput);
        await Assert.That(revertedDiagnostics).IsEmpty();
        await Assert.That(revertedGate.Type.SpecialType).IsEqualTo(SpecialType.System_Object);
        await Assert.That(reverted.GetRunResult().GeneratedTrees.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task UnrelatedSourceChange_ReuseGeneratedSourceAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public partial class Model
                              {
                                  [ExternalEvent] private void Run() { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);
        var previousSource = driver.GetRunResult().Results.Single().GeneratedSources.Single().SourceText;
        var unrelatedTree = CSharpSyntaxTree.ParseText("public class Unrelated { }", (CSharpParseOptions)input.SyntaxTrees.First().Options);
        var updated = driver.RunGeneratorsAndUpdateCompilation(
            input.AddSyntaxTrees(unrelatedTree), out var output, out var diagnostics,
            TestContext.Current!.Execution.CancellationToken);

        var currentSource = updated.GetRunResult().Results.Single().GeneratedSources.Single().SourceText;

        await GeneratorTest.AssertCompilesAsync(output);
        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(currentSource).IsSameReferenceAs(previousSource);
    }

    [Test]
    public async Task UnrelatedParseOptions_ReuseGeneratedSourceAsync()
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;
                              public partial class Model
                              {
                                  [ExternalEvent] private void Run() { }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);
        var previous = driver.GetRunResult();
        var parseOptions = ((CSharpParseOptions)input.SyntaxTrees.First().Options).WithDocumentationMode(DocumentationMode.Diagnose);
        var updated = driver.WithUpdatedParseOptions(parseOptions)
            .RunGeneratorsAndUpdateCompilation(input, out var output, out var diagnostics, TestContext.Current!.Execution.CancellationToken);

        var current = updated.GetRunResult();
        var reasons = current.Results.Single().TrackedOutputSteps.Values
            .SelectMany(static steps => steps)
            .SelectMany(static step => step.Outputs)
            .Select(static result => result.Reason)
            .ToArray();

        await GeneratorTest.AssertCompilesAsync(output);
        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(current.Results.Single().GeneratedSources.Single().SourceText).IsSameReferenceAs(previous.Results.Single().GeneratedSources.Single().SourceText);
        await Assert.That(reasons).IsNotEmpty();
        await Assert.That(reasons.All(static reason => reason == IncrementalStepRunReason.Cached)).IsTrue();
    }
}
