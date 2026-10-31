using Nice3point.Revit.Injector.Attributes;
using Nice3point.Revit.Toolkit.Tests;

[assembly: RevitClient(Id = TestsConfiguration.AddInId, Name = TestsConfiguration.AddInName, VendorId = TestsConfiguration.VendorId)]

namespace Nice3point.Revit.Toolkit.Tests;

internal static class TestsConfiguration
{
    public const string AddInId = "4C2E8A61-7F3B-4D95-B0A8-9E1D6C5F2B47";
    public const string AddInName = "Toolkit Tests";
    public const string VendorId = "Nice3point";
}
