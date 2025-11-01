using Build.Options;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Extensions.Options;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Build.Modules;

public sealed class DeleteNugetModule(IOptions<BuildOptions> buildOptions, IOptions<NuGetOptions> nuGetOptions) : Module<CommandResult[]?>
{
    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await buildOptions.Value.Versions.Values
            .SelectAsync(async version => await context.DotNet().Nuget.Delete(new DotNetNugetDeleteOptions
                {
                    PackageNamePackageVersion = $"Nice3point.Revit.Toolkit {version}",
                    ApiKey = nuGetOptions.Value.ApiKey,
                    Source = nuGetOptions.Value.Source,
                    NonInteractive = true
                }, cancellationToken),
                cancellationToken)
            .ProcessInParallel();
    }
}