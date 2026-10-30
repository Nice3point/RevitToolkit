using Nice3point.Revit.Injector.Attributes;
using Nice3point.Revit.Toolkit.Tests;

[assembly: RevitClient(Id = TestAddIn.Id, Name = TestAddIn.Name, VendorId = TestAddIn.VendorId)]

namespace Nice3point.Revit.Toolkit.Tests;

/// <summary>
///     The add-in Revit attributes every API call of the tests to.
/// </summary>
internal static class TestAddIn
{
    public const string Id = "4C2E8A61-7F3B-4D95-B0A8-9E1D6C5F2B47";
    public const string Name = "Toolkit Tests";
    public const string VendorId = "Nice3point";
}
