using System.Text;
using Build.Options;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;
using Shouldly;

namespace Build.Modules;

/// <summary>
///     Delete the NuGet packages published from the specified git reference.
/// </summary>
public sealed class DeleteNugetModule(IOptions<DeleteOptions> deleteOptions, IOptions<NuGetOptions> nuGetOptions) : Module<CommandResult[]?>
{
    protected override async Task<CommandResult[]?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var reference = deleteOptions.Value.Ref;
        reference.ShouldNotBeNullOrWhiteSpace("No git reference was specified to delete");

        var buildOptions = await ReadBuildOptionsAsync(context, reference, cancellationToken);
        buildOptions.Versions.ShouldNotBeEmpty($"No NuGet versions were found to delete at: {reference}");

        return await buildOptions.Versions.Values
            .SelectAsync(async version => await context.DotNet().Nuget.Delete(new DotNetNugetDeleteOptions
                {
                    PackageName = "Nice3point.Revit.Toolkit",
                    Version = version,
                    ApiKey = nuGetOptions.Value.ApiKey,
                    Source = nuGetOptions.Value.Source,
                    NonInteractive = true
                }, cancellationToken: cancellationToken),
                cancellationToken)
            .ProcessInParallel();
    }

    /// <summary>
    ///     Read the build options from the settings file stored at the specified git reference.
    /// </summary>
    private static async Task<BuildOptions> ReadBuildOptionsAsync(IModuleContext context, string reference, CancellationToken cancellationToken)
    {
        var showResult = await context.Git().Commands.Show(
            new GitShowOptions
            {
                Arguments = [$"{reference}:build/appsettings.json"]
            },
            new CommandExecutionOptions
            {
                LogSettings = CommandLoggingOptions.Silent
            },
            cancellationToken);

        using var settingsStream = new MemoryStream(Encoding.UTF8.GetBytes(showResult.StandardOutput));
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(settingsStream)
            .Build();

        return configuration.GetSection("Build").Get<BuildOptions>() ?? new BuildOptions();
    }
}
