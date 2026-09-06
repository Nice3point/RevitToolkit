using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Nice3point.Revit.Toolkit.SourceGenerators.Extensions;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

/// <summary>
///     Incremental source generator that produces external event wrappers
///     for methods annotated with <c>[ExternalEvent]</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed partial class ExternalEventGenerator : IIncrementalGenerator
{
    private const string GeneratorName = "Nice3point.Revit.Toolkit.SourceGenerators.ExternalEventGenerator";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownFullyQualifiedClassNames.ExternalEventAttribute.WithoutGlobalPrefix,
                static (node, _) => node is MethodDeclarationSyntax,
                static (syntaxContext, cancellationToken) => Extractor.GetMethodResult(syntaxContext, cancellationToken));

        context.ReportDiagnostics(results.Select(static (result, _) => result.Diagnostics));

        var infosWithOptions = results
            .Where(static result => result.Info is not null)
            .Select(static (result, _) => result.Info!)
            .Combine(context.ParseOptionsProvider);

        context.RegisterSourceOutput(infosWithOptions, static (sourceProductionContext, pair) =>
        {
            var (info, parseOptions) = pair;

            var languageVersion = ((CSharpParseOptions)parseOptions).LanguageVersion;
#if ROSLYN5_0_0_OR_GREATER
            var useFieldKeyword = languageVersion >= LanguageVersion.CSharp14;
#else
            var useFieldKeyword = languageVersion == LanguageVersion.Preview;
#endif

            var source = Writer.GenerateSource(info, useFieldKeyword);
            sourceProductionContext.AddSource(info.HintName, SourceText.From(source, Encoding.UTF8));
        });
    }
}
