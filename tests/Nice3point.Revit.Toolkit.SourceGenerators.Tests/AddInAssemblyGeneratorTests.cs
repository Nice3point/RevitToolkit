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
    private const string Source = "public class Model { }";
    private const string AttributeName = "Nice3point.Revit.Toolkit.Generated.AddInAssemblyAttribute";

    [Test]
    public async Task ProjectReferences_DeclareAnAttributePerAssemblyAsync()
    {
        var (driver, output) = await RunAsync(Source, projectReferences: "MyAddIn.UI,MyAddIn.Core");

        await Assert.That(driver.GetRunResult().GeneratedTrees).Count().IsEqualTo(1);
        await Assert.That(ReadListedAssemblyNames(output)).IsEquivalentTo(["MyAddIn.Core", "MyAddIn.UI"], CollectionOrdering.Matching);
    }

    [Test]
    [Arguments(" MyAddIn.Core , MyAddIn.Core,,")]
    [Arguments("MyAddIn.Core,myaddin.core")]
    [Arguments("MyAddIn.Core")]
    public async Task RepeatedPaddedOrCaseVariantNames_DeclareEachAssemblyOnceAsync(string projectReferences)
    {
        var (_, output) = await RunAsync(Source, projectReferences);

        await Assert.That(ReadListedAssemblyNames(output)).IsEquivalentTo(["MyAddIn.Core"]);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" , ")]
    public async Task NoProjectReferences_GeneratesNoSourceAsync(string? projectReferences)
    {
        var (driver, _) = await RunAsync(Source, projectReferences);

        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
    }

    [Test]
    [Arguments(LanguageVersion.CSharp7_3)]
    [Arguments(LanguageVersion.CSharp14)]
    public async Task LanguageVersion_GeneratesCompilableSourceAsync(LanguageVersion languageVersion)
    {
        var (_, output) = await RunAsync(Source, projectReferences: "MyAddIn.Core", languageVersion: languageVersion);

        await Assert.That(ReadListedAssemblyNames(output)).IsEquivalentTo(["MyAddIn.Core"]).Because("a .NET Framework project compiles with C# 7.3 by default");
    }

    [Test]
    public async Task ReferenceWithVisibleInternals_DeclaresTheAttributeWithoutConflictsAsync()
    {
        const string librarySource = """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Consumer")]""";
        var (_, library) = await RunAsync(librarySource, projectReferences: "MyAddIn.Core", assemblyName: "MyAddIn.Services");

        var (_, output) = await RunAsync(Source, projectReferences: "MyAddIn.Services", additionalReference: library.ToMetadataReference());

        await Assert.That(ReadListedAssemblyNames(output)).IsEquivalentTo(["MyAddIn.Services"]);
    }

    [Test]
    public async Task UnrelatedSourceChange_ReusesGeneratedSourceAsync()
    {
        var (driver, output) = await RunAsync(Source, projectReferences: "MyAddIn.Core");
        var previousSource = driver.GetRunResult().GeneratedTrees.Single().GetText().ToString();
        var unrelatedTree = CSharpSyntaxTree.ParseText("public class Unrelated { }", (CSharpParseOptions)output.SyntaxTrees.First().Options);

        var updated = driver.RunGenerators(output.RemoveSyntaxTrees(driver.GetRunResult().GeneratedTrees).AddSyntaxTrees(unrelatedTree), TestContext.Current!.Execution.CancellationToken);
        var outputStep = updated.GetRunResult().Results.Single().TrackedOutputSteps.Single().Value.Single();

        await Assert.That(updated.GetRunResult().GeneratedTrees.Single().GetText().ToString()).IsEqualTo(previousSource);
        await Assert.That(outputStep.Outputs.Single().Reason).IsEqualTo(IncrementalStepRunReason.Cached);
    }

    private static async Task<(GeneratorDriver Driver, Compilation Output)> RunAsync(
        [StringSyntax("C#")] string source,
        string? projectReferences,
        string assemblyName = "Consumer",
        LanguageVersion languageVersion = LanguageVersion.CSharp14,
        MetadataReference? additionalReference = null)
    {
        var cancellationToken = TestContext.Current!.Execution.CancellationToken;
        var parseOptions = new CSharpParseOptions(languageVersion);
        var references = await ReferenceAssemblies.Net.Net80.ResolveAsync(LanguageNames.CSharp, cancellationToken);
        var input = CSharpCompilation.Create(assemblyName,
            [CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: cancellationToken)],
            additionalReference is null ? references : references.Add(additionalReference),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var globalOptions = projectReferences is null
            ? ImmutableDictionary<string, string>.Empty
            : ImmutableDictionary<string, string>.Empty.Add("build_property._RevitToolkitAddInProjectReferences", projectReferences);

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
            .Where(static attribute => attribute.AttributeClass?.ToDisplayString() == AttributeName)
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
