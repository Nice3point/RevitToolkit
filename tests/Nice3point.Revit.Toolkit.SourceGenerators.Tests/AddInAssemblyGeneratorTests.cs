using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;
using TUnit.Assertions.Enums;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

public sealed class AddInAssemblyGeneratorTests
{
    private const string AttributeSource = """
                                           namespace Nice3point.Revit.Toolkit.External
                                           {
                                               [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
                                               public sealed class AddInAssemblyAttribute : System.Attribute
                                               {
                                                   public AddInAssemblyAttribute(string assemblyName) => AssemblyName = assemblyName;

                                                   public string AssemblyName { get; }
                                               }
                                           }
                                           """;

    [Test]
    public async Task ProjectReferences_DeclareAnAttributePerAssemblyAsync()
    {
        var (driver, output) = await RunAsync(AttributeSource, projectReferences: "MyAddIn.UI,MyAddIn.Core");

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Assert.That(ReadListedAssemblyNames(output)).IsEquivalentTo(["MyAddIn.Core", "MyAddIn.UI"], CollectionOrdering.Matching);
    }

    [Test]
    [Arguments(" MyAddIn.Core , MyAddIn.Core,,")]
    [Arguments("MyAddIn.Core,myaddin.core")]
    [Arguments("MyAddIn.Core")]
    public async Task RepeatedPaddedOrCaseVariantNames_DeclareEachAssemblyOnceAsync(string projectReferences)
    {
        var (_, output) = await RunAsync(AttributeSource, projectReferences);

        await Assert.That(ReadListedAssemblyNames(output)).IsEquivalentTo(["MyAddIn.Core"]);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" , ")]
    public async Task NoProjectReferences_GeneratesNoSourceAsync(string? projectReferences)
    {
        var (driver, _) = await RunAsync(AttributeSource, projectReferences);

        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
    }

    [Test]
    public async Task MissingAttributeType_GeneratesNoSourceAsync()
    {
        var (driver, _) = await RunAsync("public class Model { }", projectReferences: "MyAddIn.Core");

        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
    }

    [Test]
    public async Task UnrelatedSourceChange_ReusesGeneratedSourceAsync()
    {
        var (driver, output) = await RunAsync(AttributeSource, projectReferences: "MyAddIn.Core");
        var previousSource = driver.GetRunResult().GeneratedTrees.Single().GetText().ToString();
        var unrelatedTree = CSharpSyntaxTree.ParseText("public class Unrelated { }", (CSharpParseOptions)output.SyntaxTrees.First().Options);

        var updated = driver.RunGenerators(output.RemoveSyntaxTrees(driver.GetRunResult().GeneratedTrees).AddSyntaxTrees(unrelatedTree), TestContext.Current!.Execution.CancellationToken);
        var outputStep = updated.GetRunResult().Results.Single().TrackedOutputSteps.Single().Value.Single();

        await Assert.That(updated.GetRunResult().GeneratedTrees.Single().GetText().ToString()).IsEqualTo(previousSource);
        await Assert.That(outputStep.Outputs.Single().Reason).IsEqualTo(IncrementalStepRunReason.Cached);
    }

    private static async Task<(GeneratorDriver Driver, Compilation Output)> RunAsync([StringSyntax("C#")] string source, string? projectReferences)
    {
        var cancellationToken = TestContext.Current!.Execution.CancellationToken;
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp14);
        var references = await ReferenceAssemblies.Net.Net80.ResolveAsync(LanguageNames.CSharp, cancellationToken);
        var input = CSharpCompilation.Create("Consumer",
            [CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: cancellationToken)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var globalOptions = projectReferences is null
            ? ImmutableDictionary<string, string>.Empty
            : ImmutableDictionary<string, string>.Empty.Add("build_property.AddInProjectReferences", projectReferences);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AddInAssemblyGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            optionsProvider: new GlobalOptionsProvider(globalOptions),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGeneratorsAndUpdateCompilation(input, out var output, out var diagnostics, cancellationToken);

        await Assert.That(diagnostics).IsEmpty();
        await GeneratorTest.AssertCompilesAsync(output);
        return (driver, output);
    }

    private static IEnumerable<string> ReadListedAssemblyNames(Compilation compilation)
    {
        return compilation.Assembly.GetAttributes()
            .Where(static attribute => attribute.AttributeClass?.Name == "AddInAssemblyAttribute")
            .Select(static attribute => (string)attribute.ConstructorArguments[0].Value!);
    }

    private sealed class GlobalOptionsProvider(ImmutableDictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new DictionaryOptions(globalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return DictionaryOptions.Empty;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return DictionaryOptions.Empty;
        }
    }

    private sealed class DictionaryOptions(ImmutableDictionary<string, string> options) : AnalyzerConfigOptions
    {
        public static readonly DictionaryOptions Empty = new(ImmutableDictionary<string, string>.Empty);

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return options.TryGetValue(key, out value);
        }
    }
}
