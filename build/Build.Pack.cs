using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    Target Pack => _ => _
        .DependsOn(Clean)
        .Requires(() => ReleaseVersion)
        .Executes(() =>
        {
            var readme = CreateNugetReadme();
            try
            {
                var changelog = CreateNugetChangelog();
                foreach (var configuration in GlobBuildConfigurations())
                {
                    DotNetPack(settings => settings
                        .SetConfiguration(configuration)
                        .SetVersion(GetPackVersion(configuration))
                        .SetOutputDirectory(ArtifactsDirectory)
                        .SetVerbosity(DotNetVerbosity.minimal)
                        .SetPackageReleaseNotes(changelog));
                }
            }
            finally
            {
                RestoreReadme(readme);
            }
        });

    string GetPackVersion(string configuration)
    {
        if (PackageVersionsMap.TryGetValue(configuration, out var version)) return version;
        throw new Exception($"Can't find pack version for configuration: {configuration}");
    }

    string CreateNugetReadme()
    {
        var readmePath = Solution.Directory / "Readme.md";
        var readme = File.ReadAllText(readmePath);

        var startSymbol = "<p";
        var endSymbol = "</p>\r\n\r\n";
        var logoStartIndex = readme.IndexOf(startSymbol, StringComparison.Ordinal);
        var logoEndIndex = readme.IndexOf(endSymbol, StringComparison.Ordinal);

        var nugetReadme = readme.Remove(logoStartIndex, logoEndIndex - logoStartIndex + endSymbol.Length);
        File.WriteAllText(readmePath, nugetReadme);

        return readme;
    }

    void RestoreReadme(string readme)
    {
        var readmePath = Solution.Directory / "Readme.md";

        File.WriteAllText(readmePath, readme);
    }
}