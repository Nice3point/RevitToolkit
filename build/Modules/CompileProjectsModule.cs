using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Sourcy.DotNet;

namespace Build.Modules;

[DependsOn<ParseSolutionConfigurationsModule>]
public sealed class CompileProjectsModule : Module
{
    protected override async Task<IDictionary<string, object>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var configurations = await GetModule<ParseSolutionConfigurationsModule>();
        
        foreach (var configuration in configurations.Value!)
        {
            await SubModule(configuration, async () => await CompileAsync(context, configuration, cancellationToken));
        }

        return await NothingAsync();
    }

    private async Task<CommandResult> CompileAsync(IPipelineContext context, string configuration, CancellationToken cancellationToken)
    {
        return await context.DotNet().Build(new DotNetBuildOptions
        {
            ProjectSolution = Projects.Nice3point_Revit_Toolkit.FullName,
            Configuration = configuration,
            Verbosity = Verbosity.Minimal,
        }, cancellationToken);
    }
}