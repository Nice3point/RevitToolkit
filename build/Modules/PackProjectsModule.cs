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

[DependsOn<CleanProjectsModule>]
[DependsOn<CreatePackageReadmeModule>]
[DependsOn<CreatePackageChangelogModule>]
[DependsOn<ParseSolutionConfigurationsModule>]
public sealed class PackProjectsModule(IOptions<BuildOptions> buildOptions,IOptions<PackOptions> packOptions) : Module
{
    protected override async Task<IDictionary<string, object>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var configurations = await GetModule<ParseSolutionConfigurationsModule>();
        var changelog = await GetModule<CreatePackageChangelogModule>();
        var outputFolder = context.Git().RootDirectory.GetFolder(packOptions.Value.OutputDirectory);

        foreach (var configuration in configurations.Value!)
        {
            await SubModule(configuration, async () => await PackAsync(context, configuration, outputFolder, changelog.Value!, cancellationToken));
        }

        return await NothingAsync();
    }

    private async Task<CommandResult> PackAsync(IPipelineContext context, string configuration, string output, string changelog, CancellationToken cancellationToken)
    {
        buildOptions.Value.Versions
            .TryGetValue(configuration, out var version)
            .ShouldBeTrue($"Can't find pack version for configuration: {configuration}");

        return await context.DotNet().Pack(new DotNetPackOptions
        {
            ProjectSolution = Projects.Nice3point_Revit_Toolkit.FullName,
            Configuration = configuration,
            Verbosity = Verbosity.Minimal,
            Properties = new List<KeyValue>
            {
                ("Version", version.ToString()),
                ("PackageReleaseNotes", changelog),
            },
            OutputDirectory = output
        }, cancellationToken);
    }
}