namespace Build.Options;

[PublicAPI]
public sealed record PublishOptions
{
    public string Version { get; init; } = string.Empty;
}
