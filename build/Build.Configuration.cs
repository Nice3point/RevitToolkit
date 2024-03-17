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
            { "Release R20", "2020.2.0-preview.2.0" },
            { "Release R21", "2021.2.0-preview.2.0" },
            { "Release R22", "2022.2.0-preview.2.0" },
            { "Release R23", "2023.2.0-preview.2.0" },
            { "Release R24", "2024.1.0-preview.2.0" },
            { "Release R25", "2025.0.0-preview.2.0" },
        };
    }
}