using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;

namespace Build.Modules;

[DependsOn<UpdateReadmeModule>]
[DependsOn<PackProjectsModule>(Optional = true)]
public sealed class RestoreReadmeModule : Module
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithAlwaysRun()
        .WithSkipWhen(async context =>
        {
            var nugetReadmeModule = await context.GetModule<UpdateReadmeModule>();
            return !nugetReadmeModule.IsSuccess;
        })
        .Build();

    protected override async Task ExecuteModuleAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var nugetReadmeResult = await context.GetModule<UpdateReadmeModule>();
        if (!nugetReadmeResult.IsSuccess)
        {
            return;
        }

        var nugetReadme = nugetReadmeResult.ValueOrDefault!;
        var readmePath = context.Git().RootDirectory.GetFile("Readme.md");
        await readmePath.WriteAsync(nugetReadme, cancellationToken);
    }
}