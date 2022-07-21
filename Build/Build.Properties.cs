partial class Build
{
    readonly Dictionary<string, string> VersionMap = new()
    {
        {"Release R19", "2019.0.0"},
        {"Release R20", "2020.0.0"},
        {"Release R21", "2021.0.0"},
        {"Release R22", "2022.0.0"},
        {"Release R23", "2023.0.0"},
    };

    const string BuildConfiguration = "Release";
    const string ArtifactsFolder = "output";
}