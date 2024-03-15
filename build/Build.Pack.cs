using Nuke.Common.Git;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    Target Pack => _ => _
        .DependsOn(Compile)
        .OnlyWhenStatic(() => GitRepository.IsOnMainOrMasterBranch())
        .Executes(() =>
        {
            ValidateRelease();

            foreach (var configuration in GlobBuildConfigurations())
                DotNetPack(settings => settings
                    .SetConfiguration(configuration)
                    .SetVersion(GetPackVersion(configuration))
                    .SetOutputDirectory(ArtifactsDirectory)
                    .SetVerbosity(DotNetVerbosity.minimal)
                    .SetPackageReleaseNotes(CreateNugetChangelog()));
        });

    string GetPackVersion(string configuration)
    {
        if (VersionMap.TryGetValue(configuration, out var version)) return version;
        throw new Exception($"Can't find pack version for configuration: {configuration}");
    }

    string CreateNugetChangelog()
    {
        Assert.True(File.Exists(ChangeLogPath), $"Unable to locate the changelog file: {ChangeLogPath}");
        Log.Information("Changelog: {Path}", ChangeLogPath);

        var changelog = BuildChangelog();
        Assert.True(changelog.Length > 0, $"No version entry exists in the changelog: {Version}");

        return EscapeMsBuild(changelog.ToString());
    }

    static string EscapeMsBuild(string value)
    {
        return value
            .Replace(";", "%3B")
            .Replace(",", "%2C");
    }
}