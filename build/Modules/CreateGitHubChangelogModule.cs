using System.Text;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using ModularPipelines.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Options;
using ModularPipelines.GitHub.Extensions;

namespace Build.Modules;

[DependsOn<CreateChangelogModule>]
public sealed class CreateGitHubChangelogModule : Module<string>
{
    protected override async Task<string?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var changelogModule = await GetModule<CreateChangelogModule>();

        var changelog = await AppendGitHubCompareUrlAsync(context, changelogModule.Value!);
        return changelog.ToString();
    }

    private static async Task<StringBuilder> AppendGitHubCompareUrlAsync(IPipelineContext context, StringBuilder changelog)
    {
        var tagCommand = await context.Git().Commands.Tag(new GitTagOptions
        {
            List = true,
            Sort = "v:refname"
        });

        var tags = tagCommand.StandardOutput.Split(Environment.NewLine);
        if (tags.Length < 2) return changelog;

        var repositoryName = context.GitHub().RepositoryInfo.Identifier;
        var previousTag = tags[^2];
        var latestTag = tags[^1];

        if (changelog[^1] != '\r' || changelog[^1] != '\n') changelog.AppendLine(Environment.NewLine);
        changelog.Append("Full changelog: ");
        changelog.Append($"https://github.com/{repositoryName}/compare/{previousTag}...{latestTag}");

        return changelog;
    }
}