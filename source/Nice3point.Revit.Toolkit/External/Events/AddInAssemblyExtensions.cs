using System.Reflection;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.Internal;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Provides extension methods for <see cref="UIControlledApplication" /> and <see cref="UIApplication" /> to register add-in assemblies.
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
        /// <param name="assembly">The assembly with external event handlers of the add-in.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="assembly" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.InvalidOperationException">The method is called outside a Revit callback.</exception>
        /// <remarks>
        ///     External events of the assembly run as the active add-in on any thread.
        ///     Project references of the assembly are registered too.
        ///     <see cref="ExternalApplication" /> and <see cref="ExternalCommand" /> register their assembly automatically.
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
