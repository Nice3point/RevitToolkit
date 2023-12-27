using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    Target Pack => _ => _
        .TriggeredBy(Compile)
        .Executes(() =>
        {
            ValidateVersion();

            foreach (var configuration in GlobBuildConfigurations())
                DotNetPack(settings => settings
                    .SetConfiguration(configuration)
                    .SetVersion(GetPackVersion(configuration))
                    .SetOutputDirectory(ArtifactsDirectory)
                    .SetVerbosity(DotNetVerbosity.Minimal)
                    .SetPackageReleaseNotes(CreateNugetChangelog()));
        });

    string GetPackVersion(string configuration)
    {
        if (VersionMap.TryGetValue(configuration, out var version)) return version;
        throw new Exception($"Can't find pack version for configuration: {configuration}");
    }

    string CreateNugetChangelog()
    {
        if (!File.Exists(ChangeLogPath))
        {
            Log.Error("Unable to locate the changelog file: {Log}", ChangeLogPath);
            return string.Empty;
        }

        Log.Information("Changelog: {Path}", ChangeLogPath);

        var changelog = ReadChangelog();
        if (changelog.Length == 0)
        {
            Log.Error("No version entry exists in the changelog: {Version}", PublishVersion);
            return string.Empty;
        }

        return changelog.ToString();
    }
}