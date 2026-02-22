#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit;

/// <summary>
///     Provides unsafe accessor methods for internal Revit API members.
/// </summary>
internal static class UnsafeAccessors
{
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    internal static extern Application CreateApplication(object proxy);

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    internal static extern UIControlledApplication CreateUiControlledApplication(UIApplication uiApplication);

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "AssemblyResolve")]
    internal static extern ref ResolveEventHandler? GetAssemblyResolveField(AssemblyLoadContext context);
}
#endif
