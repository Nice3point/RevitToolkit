using Build.Options;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Shouldly;
using Sourcy.DotNet;

namespace Build.Modules;

[DependsOn<ResolveConfigurationsModule>]
[DependsOn<UpdateReadmeModule>(Optional = true)]
[DependsOn<CleanProjectsModule>(Optional = true)]
[DependsOn<GenerateNugetChangelogModule>(Optional = true)]
[DependsOn<TestProjectsModule>(Optional = true)]
public sealed class PackProjectsModule(IOptions<BuildOptions> buildOptions) : Module
{
    protected override async Task ExecuteModuleAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var configurationsResult = await context.GetModule<ResolveConfigurationsModule>();
        var changelogModule = context.GetModuleIfRegistered<GenerateNugetChangelogModule>();

        var configurations = configurationsResult.ValueOrDefault!;
        var changelogResult = changelogModule is null ? null : await changelogModule;
        var changelog = changelogResult?.ValueOrDefault;
        var outputFolder = context.Git().RootDirectory.GetFolder(buildOptions.Value.OutputDirectory);

        foreach (var configuration in configurations)
        {
            await context.SubModule(configuration, async () => await PackAsync(context, configuration, outputFolder.Path, changelog, cancellationToken));
        }
    }

    private async Task<CommandResult> PackAsync(IModuleContext context, string configuration, string output, string? changelog, CancellationToken cancellationToken)
    {
        buildOptions.Value.Versions
            .TryGetValue(configuration, out var version)
            .ShouldBeTrue($"Can't find pack version for configuration: {configuration}");

        return await context.DotNet().Pack(new DotNetPackOptions
        {
            ProjectSolution = Projects.Nice3point_Revit_Toolkit.FullName,
            Configuration = configuration,
            Properties = new List<KeyValue>
            {
                ("Version", version),
                ("PackageReleaseNotes", changelog ?? string.Empty),
            },
            Output = output
        }, cancellationToken: cancellationToken);
    }
}