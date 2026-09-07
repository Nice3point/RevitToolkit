using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

internal static class GeneratorTest
{
    private static readonly string Fixture = ReadFixture();

    public static async Task<(GeneratorDriver Driver, CSharpCompilation Input)> RunAsync(
        [StringSyntax("C#")] string source,
        string[]? diagnosticIds = null,
        LanguageVersion languageVersion = LanguageVersion.CSharp14,
        string[]? additionalSources = null,
        bool allowUnsafe = false,
        ReferenceAssemblies? referenceAssemblies = null)
    {
        var cancellationToken = TestContext.Current!.Execution.CancellationToken;
        var parseOptions = new CSharpParseOptions(languageVersion);
        var references = await (referenceAssemblies ?? ReferenceAssemblies.Net.Net80).ResolveAsync(LanguageNames.CSharp, cancellationToken);
        var trees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText(SourceText.From(Fixture, Encoding.UTF8), parseOptions, "ExternalEvents.cs", cancellationToken),
            CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), parseOptions, "Input.cs", cancellationToken)
        };

        foreach (var additionalSource in additionalSources ?? [])
        {
            trees.Add(CSharpSyntaxTree.ParseText(SourceText.From(additionalSource, Encoding.UTF8), parseOptions, $"Input{trees.Count - 1}.cs", cancellationToken));
        }

        var input = CSharpCompilation.Create("Consumer", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: allowUnsafe, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ExternalEventGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGeneratorsAndUpdateCompilation(input, out var output, out var diagnostics, cancellationToken);

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id)).IsEquivalentTo(diagnosticIds ?? []);
        await AssertCompilesAsync(output);
        return (driver, input);
    }

    public static async Task AssertCompilesAsync(Compilation compilation)
    {
        var errors = compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken)
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(static diagnostic => diagnostic.ToString());

        await Assert.That(errors).IsEmpty();
    }

    public static async Task<T> ExecuteAsync<T>(GeneratorDriver driver, CSharpCompilation input)
    {
        var compilation = input.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees);
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream, cancellationToken: TestContext.Current!.Execution.CancellationToken);
        await Assert.That(result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        stream.Position = 0;

        var loadContext = new AssemblyLoadContext(null, isCollectible: true);
        try
        {
            var assembly = loadContext.LoadFromStream(stream);
            var run = assembly.GetType("Consumer.Probe", throwOnError: true)!.GetMethod("RunAsync")!;
            var execution = (Task<T>)run.Invoke(null, null)!;
            return await execution.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.Execution.CancellationToken);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static string ReadFixture()
    {
        using var stream = typeof(GeneratorTest).Assembly.GetManifestResourceStream("Nice3point.Revit.Toolkit.SourceGenerators.Tests.Fixtures.ExternalEvents.cs")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
