using System.IO.Enumeration;
using Build.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using Shouldly;

namespace Build.Modules;

public sealed class ParseSolutionConfigurationsModule(IOptions<BuildOptions> buildOptions) : Module<string[]>
{
    protected override async Task<string[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var solutionModel = await LoadSolutionModelAsync(context, cancellationToken);
        var configurations = solutionModel.BuildTypes
            .Where(configuration => FileSystemName.MatchesSimpleExpression(buildOptions.Value.ConfigurationFilter, configuration))
            .ToArray();

        configurations.Length.ShouldBePositive("No solution configurations have been found");

        return configurations;
    }

    private static async Task<SolutionModel> LoadSolutionModelAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var solution = context.Git().RootDirectory.FindFile(file => file.Extension == ".slnx");
        if (solution is not null)
        {
            return await SolutionSerializers.SlnXml.OpenAsync(solution.GetStream(), cancellationToken);
        }

        context.Logger.LogInformation("Solution file not found. Trying to find fallback .sln");

        solution = context.Git().RootDirectory.FindFile(file => file.Extension == ".sln");
        solution.ShouldNotBeNull("Solution file not found.");

        return await SolutionSerializers.SlnFileV12.OpenAsync(solution.GetStream(), cancellationToken);
    }
}