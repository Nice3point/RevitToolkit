using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

/// <summary>
///     Incremental source generator that produces external event wrappers
///     for methods annotated with <c>[ExternalEvent]</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed partial class ExternalEventGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownFullyQualifiedClassNames.ExternalEventAttribute.WithoutGlobalPrefix,
                predicate: static (node, cancellationToken) => node is MethodDeclarationSyntax,
                transform: static (syntaxContext, cancellationToken) => Execute.GetMethodResult(syntaxContext, cancellationToken));

        var combinedResults = methodResults.Combine(context.ParseOptionsProvider);

        context.RegisterSourceOutput(combinedResults, static (sourceProductionContext, pair) =>
        {
            var (result, parseOptions) = pair;

            if (result.Diagnostics is { } diagnostics)
            {
                foreach (var diagnostic in diagnostics)
                {
                    sourceProductionContext.ReportDiagnostic(diagnostic);
                }
            }

            if (result.Info is not { } info)
            {
                return;
            }

            var languageVersion = ((CSharpParseOptions)parseOptions).LanguageVersion;
#if ROSLYN5_0_0_OR_GREATER
            var useFieldKeyword = languageVersion >= LanguageVersion.CSharp14;
#else
            var useFieldKeyword = languageVersion == LanguageVersion.Preview;
#endif

            var source = Emitter.Generate(info, useFieldKeyword);
            sourceProductionContext.AddSource(info.HintName, SourceText.From(source, Encoding.UTF8));
        });
    }
}