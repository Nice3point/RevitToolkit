using Autodesk.Revit.UI;
#if NETCOREAPP
using Nice3point.Revit.Toolkit.Helpers;
#endif

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Provide the <see cref="Autodesk.Revit.UI.IExternalCommandAvailability"/> implementation for an accessibility check for a Revit add-in External Command.
/// </summary>
[PublicAPI]
public abstract class ExternalCommandAvailability : IExternalCommandAvailability
{
#if NETCOREAPP
    private object? _isolatedInstance;
#endif

    /// <summary> Implement this method to provide control over whether your external command is enabled or disabled.</summary>
    /// <returns> Indicates whether Revit should enable or disable the corresponding external command.</returns>
    /// <remarks>
    /// This callback will be called by Revit's user interface any time there is a contextual change.
    /// Therefore, the callback must be fast and is not permitted to modify the active document and be blocking in any way.
    /// </remarks>
    /// <param name="applicationData"> An ApplicationServices.Application object which contains reference to Application
    /// needed by external command. </param>
    /// <param name="selectedCategories"> An list of categories of the elements which have been selected in Revit in the active document,
    /// or an empty set if no elements are selected or there is no active document. </param>
    public abstract bool SetCommandAvailability(UIApplication applicationData, CategorySet selectedCategories);

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
    public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
    {
#if NETCOREAPP
        if (_isolatedInstance is not null)
        {
            return AddinLoadContext.Invoke(_isolatedInstance, nameof(IsCommandAvailable), applicationData, selectedCategories);
        }

        var currentType = GetType();

        if (!AddinLoadContext.CheckAccess(currentType))
        {
            var dependenciesProvider = AddinLoadContext.GetDependenciesProvider(currentType);
            _isolatedInstance = dependenciesProvider.CreateInstance(currentType);

            return AddinLoadContext.Invoke(_isolatedInstance, nameof(IsCommandAvailable), applicationData, selectedCategories);
        }

        return SetCommandAvailability(applicationData, selectedCategories);
#else
        return SetCommandAvailability(applicationData, selectedCategories);
#endif
    }
}