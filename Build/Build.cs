using System.Text;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.Git;
using Serilog;

sealed partial class Build : NukeBuild
{
    string Version;
    string[] Configurations;
    Dictionary<string, string> VersionMap;

    [Secret] [Parameter] string GitHubToken;
    [Secret] [Parameter] string NugetApiKey;
    [GitRepository] readonly GitRepository GitRepository;
    [Solution(GenerateProjects = true)] readonly Solution Solution;

    public static int Main() => Execute<Build>(x => x.Clean);

    void ValidateVersion()
    {
        var tags = GitTasks.Git("describe --tags --abbrev=0", logInvocation: false, logOutput: false);
        if (tags.Count == 0) return;

        Assert.False(tags.Last().Text == PublishVersion, $"A Release with the specified tag already exists in the repository: {PublishVersion}");
        Log.Information("Version: {Version}", PublishVersion);
    }

    StringBuilder ReadChangelog()
    {
        const string separator = "# ";

        var hasEntry = false;
        var changelog = new StringBuilder();
        foreach (var line in File.ReadLines(ChangeLogPath))
        {
            if (hasEntry)
            {
                if (line.StartsWith(separator)) break;

                changelog.AppendLine(line);
                continue;
            }

            if (line.StartsWith(separator) && line.Contains(PublishVersion))
            {
                hasEntry = true;
            }
        }

        TrimEmptyLines(changelog);
        return changelog;
    }

    static void TrimEmptyLines(StringBuilder changelog)
    {
        while (changelog[^1] == '\r' || changelog[^1] == '\n')
        {
            changelog.Remove(changelog.Length - 1, 1);
        }

        while (changelog[0] == '\r' || changelog[0] == '\n')
        {
            changelog.Remove(0, 1);
        }
    }
}