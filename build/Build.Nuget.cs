using Nuke.Common.Git;
using Nuke.Common.Tools.DotNet;
using RevitExtensions.Build.Tools;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static RevitExtensions.Build.Tools.DotNetExtendedTasks;

partial class Build
{
    const string NugetApiUrl = "https://api.nuget.org/v3/index.json";

    Target NuGetPush => _ => _
        .DependsOn(Pack)
        .Requires(() => NugetApiKey)
        .OnlyWhenStatic(() => IsLocalBuild && GitRepository.IsOnMainOrMasterBranch())
        .Executes(() =>
        {
            foreach (var package in ArtifactsDirectory.GlobFiles("*.nupkg"))
                DotNetNuGetPush(settings => settings
                    .SetTargetPath(package)
                    .SetSource(NugetApiUrl)
                    .SetApiKey(NugetApiKey));
        });

    Target NuGetDelete => _ => _
        .Requires(() => NugetApiKey)
        .OnlyWhenStatic(() => IsLocalBuild && GitRepository.IsOnMainOrMasterBranch())
        .Executes(() =>
        {
            foreach (var (config, version) in VersionMap)
            {
                DotNetNuGetDelete(settings => settings
                    .SetPackage("Nice3point.Revit.Toolkit")
                    .SetVersion(version)
                    .SetSource(NugetApiUrl)
                    .SetApiKey(NugetApiKey)
                    .EnableNonInteractive());
            }
        });
}