using System.Reflection;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.Internal;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Provides extension methods for <see cref="UIControlledApplication" /> and <see cref="UIApplication" /> to register the assemblies of an add-in.
/// </summary>
[PublicAPI]
public static class AddInAssemblyExtensions
{
    /// <summary>
    ///     Defines extensions for <see cref="UIControlledApplication" />.
    /// </summary>
    /// <param name="application">The application passed to the add-in at startup.</param>
    extension(UIControlledApplication application)
    {
        /// <summary>
        ///     Registers the assembly as part of the active add-in.
        /// </summary>
        /// <param name="assembly">The assembly that declares external event handlers of the add-in.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="assembly" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.InvalidOperationException">The method is called outside a Revit callback.</exception>
        /// <remarks>
        ///     An external event whose handler is declared in the assembly runs as the active add-in, wherever the event is created.
        ///     The registration replaces the add-in the toolkit captured for the assembly earlier.
        ///     The project references the toolkit lists for the assembly at build time run as the same add-in.
        ///     A Revit version without the native add-in data members ignores the call.
        /// </remarks>
        public void RegisterAddInAssembly(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            AddInContext.Register(assembly);
        }
    }

    /// <summary>
    ///     Defines extensions for <see cref="UIApplication" />.
    /// </summary>
    /// <param name="application">The application passed to a command or an event handler.</param>
    extension(UIApplication application)
    {
        /// <inheritdoc cref="AddInAssemblyExtensions.RegisterAddInAssembly(UIControlledApplication, Assembly)" />
        public void RegisterAddInAssembly(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            AddInContext.Register(assembly);
        }
    }
}
