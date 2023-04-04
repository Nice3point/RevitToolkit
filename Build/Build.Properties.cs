partial class Build
{
    const string BuildConfiguration = "Release";
    const string ArtifactsFolder = "output";

    readonly Dictionary<string, string> VersionMap = new()
    {
        {"Release R20", "2020.1.0"},
        {"Release R21", "2021.1.0"},
        {"Release R22", "2022.1.0"},
        {"Release R23", "2023.1.0"},
        {"Release R24", "2024.0.0"}
    };
}