using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.External.CommandControllers;

/// <summary>
///     Controller providing permanent accessibility for External Command invocation
/// </summary>
[PublicAPI]
public sealed class AvailableCommandController : IExternalCommandAvailability
{
    /// <summary>
    ///     The command is always available for execution on the ribbon
    /// </summary>
    public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
    {
        return true;
    }
}