using JetBrains.Annotations;
using Nuke.Common.Tooling;

namespace RevitExtensions.Build.Tools;

[PublicAPI]
public static class DotNetNuGetDeleteSettingsExtensions
{
    public static T SetPackage<T>(this T toolSettings, string targetPath) where T : DotNetNuGetDeleteSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.Package = targetPath;
        return toolSettings;
    }

    public static T SetVersion<T>(this T toolSettings, string version) where T : DotNetNuGetDeleteSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.Version = version;
        return toolSettings;
    }

    public static T SetSource<T>(this T toolSettings, string source) where T : DotNetNuGetDeleteSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.Source = source;
        return toolSettings;
    }

    public static T SetApiKey<T>(this T toolSettings, string apiKey) where T : DotNetNuGetDeleteSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.ApiKey = apiKey;
        return toolSettings;
    }

    public static T EnableInteractive<T>(this T toolSettings) where T : DotNetNuGetDeleteSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.Interactive = true;
        return toolSettings;
    }

    public static T EnableNonInteractive<T>(this T toolSettings) where T : DotNetNuGetDeleteSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.NonInteractive = true;
        return toolSettings;
    }
}