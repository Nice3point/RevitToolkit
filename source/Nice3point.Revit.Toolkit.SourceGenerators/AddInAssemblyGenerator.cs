using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Nice3point.Revit.Toolkit.SourceGenerators.AddInAssemblies;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

/// <summary>
///     Incremental source generator that declares an <c>[assembly: AddInAssembly]</c> attribute for each project reference.
/// </summary>
/// <remarks>
///     The package targets list the project references in a build property, transitive ones included.
///     <para>A project without project references generates no source.</para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class AddInAssemblyGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyNames = context.AnalyzerConfigOptionsProvider
            .Select(static (optionsProvider, _) => AddInProjectReferences.Read(optionsProvider.GlobalOptions));

        context.RegisterSourceOutput(assemblyNames, static (sourceProductionContext, projectReferences) =>
        {
            if (projectReferences.IsEmpty)
            {
                return;
            }

            var generatedSource = AddInAssemblyWriter.GenerateSource(projectReferences);
            sourceProductionContext.AddSource(AddInAssemblyWriter.HintName, SourceText.From(generatedSource, Encoding.UTF8));
        });
    }
}
