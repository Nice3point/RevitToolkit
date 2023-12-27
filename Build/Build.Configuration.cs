sealed partial class Build
{
    string PublishVersion => Version ??= VersionMap.Values.Last();
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "output";
    readonly AbsolutePath ChangeLogPath = RootDirectory / "Changelog.md";

    protected override void OnBuildInitialized()
    {
        Configurations =
        [
            "Release*"
        ];

        VersionMap = new()
        {
            { "Release R20", "2020.0.10" },
            { "Release R21", "2021.0.10" },
            { "Release R22", "2022.0.10" },
            { "Release R23", "2023.0.10" },
            { "Release R24", "2024.0.0" }
        };
    }
}