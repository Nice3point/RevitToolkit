using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Nice3point.Revit.Toolkit.SourceGenerators.AddInAssemblies;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

/// <summary>
///     Incremental source generator that declares an <c>[assembly: AddInAssembly]</c> attribute for every project reference of the project.
/// </summary>
/// <remarks>
///     The toolkit MSBuild targets pass the project references, transitive ones included, as the <c>AddInProjectReferences</c> build property.
///     <para>A compilation without the accessible <c>AddInAssemblyAttribute</c> type of the toolkit generates no source.</para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class AddInAssemblyGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyNames = context.AnalyzerConfigOptionsProvider
            .Select(static (optionsProvider, _) => AddInProjectReferences.Read(optionsProvider.GlobalOptions));

        var attributeAvailability = context.CompilationProvider
            .Select(static (compilation, _) => compilation.GetTypeByMetadataName(AddInProjectReferences.AddInAssemblyAttribute.WithoutGlobalPrefix) is { } attributeType &&
                                               compilation.IsSymbolAccessibleWithin(attributeType, compilation.Assembly));

        context.RegisterSourceOutput(assemblyNames.Combine(attributeAvailability), static (sourceProductionContext, generationInput) =>
        {
            var (projectReferences, isAttributeAvailable) = generationInput;
            if (!isAttributeAvailable || projectReferences.IsEmpty)
            {
                return;
            }

            var generatedSource = AddInAssemblyWriter.GenerateSource(projectReferences);
            sourceProductionContext.AddSource(AddInAssemblyWriter.HintName, SourceText.From(generatedSource, Encoding.UTF8));
        });
    }
}
