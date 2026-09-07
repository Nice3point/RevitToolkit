using System.Runtime.CompilerServices;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

internal static class TestsConfiguration
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        VerifySourceGenerators.Initialize();
        UseProjectRelativeDirectory("Snapshots");
    }
}
