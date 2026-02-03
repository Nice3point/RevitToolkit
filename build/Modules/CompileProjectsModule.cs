using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Sourcy.DotNet;

namespace Build.Modules;

[DependsOn<ResolveConfigurationsModule>]
public sealed class CompileProjectsModule : Module
{
    protected override async Task ExecuteModuleAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var configurationsResult = await context.GetModule<ResolveConfigurationsModule>();
        var configurations = configurationsResult.ValueOrDefault!;

        foreach (var configuration in configurations)
        {
            await context.SubModule(configuration, async () => await CompileAsync(context, configuration, cancellationToken));
        }
    }

    private static async Task<CommandResult> CompileAsync(IModuleContext context, string configuration, CancellationToken cancellationToken)
    {
        return await context.DotNet().Build(new DotNetBuildOptions
        {
            ProjectSolution = Projects.Nice3point_Revit_Toolkit.FullName,
            Configuration = configuration,
        }, cancellationToken: cancellationToken);
    }
}