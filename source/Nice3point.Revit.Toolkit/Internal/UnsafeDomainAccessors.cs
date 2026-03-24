#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Nice3point.Revit.Toolkit.Internal;

/// <summary>
///     Provides unsafe accessor methods for internal Revit API members.
/// </summary>
internal static class UnsafeDomainAccessors
{
    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "AssemblyResolve")]
    internal static extern ref ResolveEventHandler? GetAssemblyResolveField(AssemblyLoadContext context);
}
#endif