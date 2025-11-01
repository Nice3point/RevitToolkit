using System.ComponentModel.DataAnnotations;

namespace Build.Options;

public sealed class BuildOptions
{
    [Required] public string ConfigurationFilter { get; init; } = null!;
    [Required] public string Version { get; init; } = null!;
    [Required] public Dictionary<string, Version> Versions { get; init; } = null!;
}