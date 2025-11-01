using System.ComponentModel.DataAnnotations;

namespace Build.Options;

public sealed class PackOptions
{
    [Required] public string OutputDirectory { get; init; } = null!;
}