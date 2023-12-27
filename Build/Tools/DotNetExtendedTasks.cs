using Nuke.Common.Tooling;
using RevitToolkit.Build.Tools;

namespace RevitExtensions.Build.Tools;

public static class DotNetExtendedTasks
{
    public static IReadOnlyCollection<Output> DotNetNuGetDelete(Configure<DotNetNuGetDeleteSettings> configurator)
    {
        return DotNetNuGetDelete(configurator(new DotNetNuGetDeleteSettings()));
    }

    public static IReadOnlyCollection<Output> DotNetNuGetDelete(DotNetNuGetDeleteSettings toolSettings = null)
    {
        toolSettings = toolSettings ?? new DotNetNuGetDeleteSettings();
        using var process = ProcessTasks.StartProcess(toolSettings);
        process.AssertZeroExitCode();
        return process.Output;
    }
}