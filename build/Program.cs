using Build.Modules;
using Build.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModularPipelines.Extensions;
using ModularPipelines.Host;

await PipelineHostBuilder.Create()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, collection) =>
    {
        collection.AddOptions<BuildOptions>().Bind(context.Configuration.GetSection("Build")).ValidateDataAnnotations();

        if (args.Contains("delete-nuget"))
        {
            collection.AddOptions<NuGetOptions>().Bind(context.Configuration.GetSection("NuGet")).ValidateDataAnnotations();
            collection.AddModule<DeleteNugetModule>();
            return;
        }

        collection.AddModule<ParseSolutionConfigurationsModule>();

        if (args.Length == 0)
        {
            collection.AddModule<CompileProjectsModule>();
            return;
        }

        if (args.Contains("pack"))
        {
            collection.AddOptions<PackOptions>().Bind(context.Configuration.GetSection("Pack")).ValidateDataAnnotations();

            collection.AddModule<CleanProjectsModule>();
            collection.AddModule<CreateChangelogModule>();
            collection.AddModule<CreatePackageChangelogModule>();
            collection.AddModule<CreatePackageReadmeModule>();
            collection.AddModule<PackProjectsModule>();
            collection.AddModule<RestoreReadmeModule>();
        }

        if (args.Contains("publish"))
        {
            if (!context.HostingEnvironment.IsProduction())
            {
                throw new InvalidOperationException("Publish can only be run in production");
            }

            collection.AddOptions<NuGetOptions>().Bind(context.Configuration.GetSection("NuGet")).ValidateDataAnnotations();

            collection.AddModule<CreateGitHubChangelogModule>();
            collection.AddModule<PublishNugetModule>();
            collection.AddModule<PublishGithubModule>();
        }
    })
    .ExecutePipelineAsync();