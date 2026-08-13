#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.Internal;

/// <summary>
///     Provides unsafe accessor methods for internal Revit API members.
/// </summary>
internal static class UnsafeUiAccessors
{
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    internal static extern UIControlledApplication CreateUiControlledApplication(UIApplication uiApplication);
}
#endif
