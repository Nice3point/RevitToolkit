#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using Autodesk.Revit.ApplicationServices;

namespace Nice3point.Revit.Toolkit.Internal;

/// <summary>
///     Provides unsafe accessor methods for internal Revit API members.
/// </summary>
internal static class UnsafeAccessors
{
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    internal static extern Application CreateApplication(object proxy);
}
#endif
