using System.Diagnostics;
using System.Reflection;

namespace Nice3point.Revit.Toolkit.External.Internal;

internal static class Resolvers
{
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