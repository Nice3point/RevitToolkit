using System.IO;
using System.Reflection;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace Nice3point.Revit.Toolkit.Helpers;

/// <summary>
///     Provides methods to resolve dependencies.
/// </summary>
/// <example>
///     <code lang="csharp">
/// try
/// {
///     var elementType = typeof(T);
///     ResolveHelper.BeginAssemblyResolve(elementType);
///     return (T)Activator.CreateInstance(elementType);
/// }
/// finally
/// {
///     ResolveHelper.EndAssemblyResolve();
/// }
/// </code>
/// </example>
[PublicAPI]
public static class ResolveHelper
{
    private static string? _moduleDirectory;
    private static object? _domainResolvers;

    /// <summary>
    ///     Subscribes the current domain to resolve dependencies for the type.
    /// </summary>
    /// <typeparam name="T">Type, to search for dependencies in the directory where this type is defined.</typeparam>
    /// <remarks>
    ///     Dependencies are searched in a directory of the specified type.
    ///     At the time of dependency resolution, all other dependency resolution methods for the domain are disabled,
    ///     this requires calling <see cref="EndAssemblyResolve" /> immediately after executing user code where dependency failures occur.
    /// </remarks>
    public static void BeginAssemblyResolve<T>()
    {
        BeginAssemblyResolve(typeof(T));
    }

    /// <summary>
    ///     Subscribes the current domain to resolve dependencies for the type.
    /// </summary>
    /// <param name="type">Type, to search for dependencies in the directory where this type is defined.</param>
    /// <remarks>
    ///     Dependencies are searched in a directory of the specified type.
    ///     At the time of dependency resolution, all other dependency resolution methods for the domain are disabled,
    ///     this requires calling <see cref="EndAssemblyResolve" /> immediately after executing user code where dependency failures occur.
    /// </remarks>
    public static void BeginAssemblyResolve(Type type)
    {
        if (_domainResolvers is not null) return;
        if (type.Module.FullyQualifiedName == "<Unknown>") return;

#if NETCOREAPP
        var loadContextType = typeof(AssemblyLoadContext);
        var resolversField = loadContextType.GetField("AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
        var resolvers = resolversField.GetValue(null);
        resolversField.SetValue(null, null);
#else
        var domainType = AppDomain.CurrentDomain.GetType();
        var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        var resolvers = resolversField.GetValue(AppDomain.CurrentDomain);
        resolversField.SetValue(AppDomain.CurrentDomain, null);
#endif

        _domainResolvers = resolvers;
        _moduleDirectory = Path.GetDirectoryName(type.Module.FullyQualifiedName);

        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    /// <summary>
    ///     Unsubscribes the current domain to resolve dependencies for the type.
    /// </summary>
    public static void EndAssemblyResolve()
    {
        if (_domainResolvers is null) return;

#if NETCOREAPP
        var loadContextType = typeof(AssemblyLoadContext);
        var resolversField = loadContextType.GetField("AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
        resolversField.SetValue(null, _domainResolvers);
#else
        var domainType = AppDomain.CurrentDomain.GetType();
        var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        resolversField.SetValue(AppDomain.CurrentDomain, _domainResolvers);
#endif

        _domainResolvers = null;
        _moduleDirectory = null;
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        var assemblyPath = Path.Combine(_moduleDirectory!, $"{assemblyName}.dll");
        if (!File.Exists(assemblyPath)) return null;

        return Assembly.LoadFrom(assemblyPath);
    }
}