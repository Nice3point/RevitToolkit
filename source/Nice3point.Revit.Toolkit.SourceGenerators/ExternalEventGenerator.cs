using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Nice3point.Revit.Toolkit.SourceGenerators.Diagnostics;
using Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents;
using Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents.Analysis;
using Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents.Emission;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

/// <summary>
///     Incremental source generator that produces external event wrappers
///     for methods annotated with <c>[ExternalEvent]</c>.
/// </summary>
/// <remarks>
///     Generic containing types retain their type parameters and constraints in generated members.
///     <para>Inaccessible nested types use generated argument records directly without top-level convenience extensions.</para>
///     <para>Instance handlers on structs capture the receiver value when an event property is first initialized.</para>
///     <para>Concurrent access initializes each event property once; failed initialization can be retried.</para>
///     <para>Unsupported method signatures and conflicting generated member names produce diagnostics without emitting wrappers.</para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ExternalEventGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodAnalyses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ExternalEventTypeNames.ExternalEventAttribute.WithoutGlobalPrefix,
                static (node, _) => node is MethodDeclarationSyntax,
                static (syntaxContext, cancellationToken) => ExternalEventExtractor.AnalyzeMethod(syntaxContext, cancellationToken));

        context.ReportDiagnostics(methodAnalyses.Select(static (methodAnalysis, _) => methodAnalysis.Diagnostics));

        var languageFeatures = context.ParseOptionsProvider.Select(static (parseOptions, _) =>
        {
            var languageVersion = ((CSharpParseOptions)parseOptions).LanguageVersion;
#if ROSLYN5_0_0_OR_GREATER
            var useFieldKeyword = languageVersion >= LanguageVersion.CSharp14;
#else
            var useFieldKeyword = languageVersion == LanguageVersion.Preview;
#endif
            return (UseFieldKeyword: useFieldKeyword, SupportsLockType: languageVersion >= LanguageVersion.CSharp13);
        });

        var lockTypeAvailability = context.CompilationProvider.Select(static (compilation, _) =>
            compilation.GetTypeByMetadataName("System.Threading.Lock") is { TypeKind: TypeKind.Class } lockType &&
            compilation.IsSymbolAccessibleWithin(lockType, compilation.Assembly));
        var generationFeatures = languageFeatures.Combine(lockTypeAvailability)
            .Select(static (features, _) => (features.Left.UseFieldKeyword, UseLockType: features.Left.SupportsLockType && features.Right));

        var eventDefinitionsWithFeatures = methodAnalyses
            .Where(static methodAnalysis => methodAnalysis.Definition is not null)
            .Select(static (methodAnalysis, _) => methodAnalysis.Definition!)
            .Combine(generationFeatures);

        context.RegisterSourceOutput(eventDefinitionsWithFeatures, static (sourceProductionContext, generationInput) =>
        {
            var (eventDefinition, features) = generationInput;
            var generatedSource = ExternalEventWriter.GenerateSource(eventDefinition, features.UseFieldKeyword, features.UseLockType);
            sourceProductionContext.AddSource(eventDefinition.HintName, SourceText.From(generatedSource, Encoding.UTF8));
        });
    }
}
