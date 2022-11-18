using System.Diagnostics;
using System.Reflection;

namespace Nice3point.Revit.Toolkit.Helpers;

/// <summary>
///     Provides handlers to resolve dependencies
/// </summary>
[PublicAPI]
public static class ResolveHelper
{
    /// <summary>
    ///     Represents a method that handles the <see cref="System.AppDomain.AssemblyResolve" /> event of an <see cref="System.AppDomain" />
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="args">The event data</param>
    /// <returns>The assembly that resolves the type, assembly, or resource; or <see langword="null" /> if the assembly cannot be resolved</returns>
    /// <remarks>
    ///     Optimized assembly resolver is enabled by default for
    ///     <see cref="Nice3point.Revit.Toolkit.External.ExternalApplication" /> and <see cref="Nice3point.Revit.Toolkit.External.ExternalCommand" />
    /// </remarks>
    public static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var frames = new StackTrace().GetFrames();
        if (frames is null) return null;

        var assemblyName = new AssemblyName(args.Name).Name;
        var directoryHistory = new List<string>();

        for (var i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            var method = frame.GetMethod();
            if (method.DeclaringType is null) continue;

            var assembliesDirectory = Path.GetDirectoryName(method.DeclaringType.Assembly.Location)!;
            if (directoryHistory.Contains(assembliesDirectory)) continue;

            directoryHistory.Add(assembliesDirectory);
            var assemblies = Directory.EnumerateFiles(assembliesDirectory, "*.dll");
            foreach (var assembly in assemblies)
                if (assemblyName == Path.GetFileNameWithoutExtension(assembly))
                    return Assembly.LoadFile(assembly);
        }

        return null;
    }

    internal static Assembly ResolveAssembly(string callerMethod, ResolveEventArgs arguments, ref string assembliesDirectory)
    {
        if (assembliesDirectory is null)
        {
            var frames = new StackTrace().GetFrames();
            if (frames is not null)
                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    if (method.Name == callerMethod && method.IsVirtual && method.DeclaringType is not null)
                    {
                        assembliesDirectory = Path.GetDirectoryName(method.DeclaringType.Assembly.Location)!;
                        break;
                    }
                }

            if (assembliesDirectory is null) return null;
        }

        var assemblyName = new AssemblyName(arguments.Name).Name;
        var assemblies = Directory.EnumerateFiles(assembliesDirectory, "*.dll");
        foreach (var assembly in assemblies)
            if (assemblyName == Path.GetFileNameWithoutExtension(assembly))
                return Assembly.LoadFile(assembly);

        return null;
    }
}