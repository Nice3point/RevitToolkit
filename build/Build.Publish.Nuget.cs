using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    const string NugetSource = "https://api.nuget.org/v3/index.json";
    [Parameter] [Secret] string NugetApiKey = EnvironmentInfo.GetVariable("NUGET_API_KEY");

    Target PublishNuget => _ => _
        .DependsOn(Pack, PublishGitHub)
        .Requires(() => NugetApiKey)
        .Executes(() =>
        {
            foreach (var package in ArtifactsDirectory.GlobFiles("*.nupkg"))
            {
                DotNetNuGetPush(settings => settings
                    .SetTargetPath(package)
                    .SetSource(NugetSource)
                    .SetApiKey(NugetApiKey));
            }
        });

    Target DeleteNuGet => _ => _
        .Requires(() => NugetApiKey)
        .OnlyWhenStatic(() => IsLocalBuild)
        .Executes(() =>
        {
            foreach (var versionPair in PackageVersionsMap)
            {
                DotNetNuGetDelete(settings => settings
                    .SetPackageId("Nice3point.Revit.Toolkit")
                    .SetPackageVersion(versionPair.Value)
                    .SetSource(NugetSource)
                    .SetApiKey(NugetApiKey)
                    .EnableNonInteractive());
            }
        });
}