using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

sealed partial class Build
{
    Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            foreach (var configuration in GlobBuildConfigurations())
            {
                DotNetBuild(settings => settings
                    .SetConfiguration(configuration)
                    .SetVerbosity(DotNetVerbosity.minimal));
            }
        });
}