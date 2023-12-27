using JetBrains.Annotations;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace RevitExtensions.Build.Tools;

[PublicAPI]
[Serializable]
public class DotNetNuGetDeleteSettings : ToolSettings
{
    public override string ProcessToolPath => base.ProcessToolPath ?? DotNetTasks.DotNetPath;
    public virtual string Package { get; internal set; }
    public virtual string Version { get; internal set; }
    public virtual string Source { get; internal set; }
    public virtual string ApiKey { get; internal set; }
    public virtual bool? ForceEnglishOutput { get; internal set; }
    public virtual bool? Interactive { get; internal set; }
    public virtual bool? NonInteractive { get; internal set; }
    public virtual bool? NoServiceEndpoint { get; internal set; }

    protected override Arguments ConfigureProcessArguments(Arguments arguments)
    {
        arguments
            .Add("nuget delete")
            .Add("{value}", Package)
            .Add("{value}", Version)
            .Add("--force-english-output", ForceEnglishOutput)
            .Add("--interactive", Interactive)
            .Add("--api-key {value}", ApiKey, secret: true)
            .Add("--no-service-endpoint", NoServiceEndpoint)
            .Add("--non-interactive", NonInteractive)
            .Add("--source {value}", Source);
        return base.ConfigureProcessArguments(arguments);
    }
}