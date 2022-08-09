partial class Build
{
    const string BuildConfiguration = "Release";
    const string ArtifactsFolder = "output";

    readonly Dictionary<string, string> VersionMap = new()
    {
        {"Release R19", "2019.0.5"},
        {"Release R20", "2020.0.5"},
        {"Release R21", "2021.0.5"},
        {"Release R22", "2022.0.5"},
        {"Release R23", "2023.0.5"}
    };
}