using ModularPipelines.Attributes;

namespace Build.Options;

[PublicAPI]
public sealed record NuGetOptions
{
    [SecretValue] public string? ApiKey { get; init; }
    public string Source { get; init; } = "https://api.nuget.org/v3/index.json";
}
