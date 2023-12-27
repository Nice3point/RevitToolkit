using System.Text;
using Nuke.Common.Git;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitHub;
using Octokit;
using Serilog;

sealed partial class Build
{
    Target PublishGitHub => _ => _
        .TriggeredBy(Pack)
        .Requires(() => GitHubToken)
        .Requires(() => GitRepository)
        .OnlyWhenStatic(() => IsServerBuild && GitRepository.IsOnMainOrMasterBranch())
        .Executes(async () =>
        {
            GitHubTasks.GitHubClient = new GitHubClient(new ProductHeaderValue(Solution.Name))
            {
                Credentials = new Credentials(GitHubToken)
            };

            var gitHubName = GitRepository.GetGitHubName();
            var gitHubOwner = GitRepository.GetGitHubOwner();

            ValidateVersion();

            var artifacts = Directory.GetFiles(ArtifactsDirectory, "*");
            var changelog = CreateGithubChangelog();

            var newRelease = new NewRelease(PublishVersion)
            {
                Name = PublishVersion,
                Body = changelog,
                TargetCommitish = GitRepository.Commit
            };

            var release = await GitHubTasks.GitHubClient.Repository.Release.Create(gitHubOwner, gitHubName, newRelease);
            await UploadArtifactsAsync(release, artifacts);
        });

    string CreateGithubChangelog()
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

        WriteCompareUrl(changelog);
        return changelog.ToString();
    }

    void WriteCompareUrl(StringBuilder changelog)
    {
        var tags = GitTasks.Git("describe --tags --abbrev=0", logInvocation: false, logOutput: false);
        if (tags.Count == 0) return;

        changelog.Append("Full changelog: ");
        changelog.Append(GitRepository.GetGitHubCompareTagsUrl(PublishVersion, tags.Last().Text));
    }

    static async Task UploadArtifactsAsync(Release createdRelease, IEnumerable<string> artifacts)
    {
        foreach (var file in artifacts)
        {
            var releaseAssetUpload = new ReleaseAssetUpload
            {
                ContentType = "application/x-binary",
                FileName = Path.GetFileName(file),
                RawData = File.OpenRead(file)
            };

            await GitHubTasks.GitHubClient.Repository.Release.UploadAsset(createdRelease, releaseAssetUpload);
            Log.Information("Artifact: {Path}", file);
        }
    }
}