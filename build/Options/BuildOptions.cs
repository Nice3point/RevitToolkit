namespace Build.Options;

[Serializable]
public sealed record BuildOptions
{
    public Dictionary<string, string> Versions { get; init; } = [];
    public string OutputDirectory { get; init; } = "output";
}