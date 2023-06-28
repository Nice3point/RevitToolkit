using Nuke.Common.Git;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using RevitToolkit.Build.Tools;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static RevitToolkit.Build.Tools.DotNetExtendedTasks;

partial class Build
{
    const string NugetApiUrl = "https://api.nuget.org/v3/index.json";
    [Secret] [Parameter] string NugetApiKey;

    Target NuGetPush => _ => _
        .Requires(() => NugetApiKey)
        .Executes(() =>
        {
            ArtifactsDirectory.GlobFiles("*.nupkg")
                .ForEach(package =>
                {
                    DotNetNuGetPush(settings => settings
                        .SetTargetPath(package)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NugetApiKey));
                });
        });

    Target NuGetDelete => _ => _
        .Requires(() => NugetApiKey)
        .Executes(() =>
        {
            var versions = new List<string>
            {
                "2020.0.12",
                "2021.0.12",
                "2022.0.12",
                "2023.0.12"
            };

            foreach (var version in versions)
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