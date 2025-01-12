using Nuke.Common.CI.GitHubActions;

sealed partial class Build
{
    [Parameter] string ReleaseVersion = GitHubActions.Instance?.RefName;

    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "output";
    readonly AbsolutePath ChangeLogPath = RootDirectory / "Changelog.md";

    protected override void OnBuildInitialized()
    {
        Configurations =
        [
            "Release*"
        ];

        PackageVersionsMap = new()
        {
            { "Release R20", "2020.2.4-preview.1.0" },
            { "Release R21", "2021.2.4-preview.1.0" },
            { "Release R22", "2022.2.4-preview.1.0" },
            { "Release R23", "2023.2.4-preview.1.0" },
            { "Release R24", "2024.1.4-preview.1.0" },
            { "Release R25", "2025.0.4-preview.1.0" }
        };
    }
}