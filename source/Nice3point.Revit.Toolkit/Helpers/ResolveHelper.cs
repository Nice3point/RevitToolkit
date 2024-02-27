using System.Reflection;

namespace Nice3point.Revit.Toolkit.Helpers;

/// <summary>
///     Provides methods to resolve dependencies
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
    private static string _moduleDirectory;
    private static object _domainResolvers;

    /// <summary>
    ///     Subscribes the current domain to resolve dependencies for the type.
    /// </summary>
    /// <typeparam name="T">Type, to search for dependencies in the directory where this type is defined</typeparam>
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
    /// <param name="type">Type, to search for dependencies in the directory where this type is defined</param>
    /// <remarks>
    ///     Dependencies are searched in a directory of the specified type.
    ///     At the time of dependency resolution, all other dependency resolution methods for the domain are disabled,
    ///     this requires calling <see cref="EndAssemblyResolve" /> immediately after executing user code where dependency failures occur.
    /// </remarks>
    public static void BeginAssemblyResolve(Type type)
    {
        if (_domainResolvers is not null) return;
        
        // when module was loaded by refAssembly (binary) FullyQualifiedName equal "<Unknown>"
        // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.module.fullyqualifiedname?view=netframework-4.8
        if (type.Module.FullyQualifiedName.Equals("<Unknown>"))
        {
            return;
        }

        var domainType = AppDomain.CurrentDomain.GetType();
        var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        var resolvers = resolversField.GetValue(AppDomain.CurrentDomain);
        resolversField.SetValue(AppDomain.CurrentDomain, null);

        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        _domainResolvers = resolvers;
        _moduleDirectory = Path.GetDirectoryName(type.Module.FullyQualifiedName);
    }

    /// <summary>
    ///     Unsubscribes the current domain to resolve dependencies for the type.
    /// </summary>
    public static void EndAssemblyResolve()
    {
        if (_domainResolvers is null) return;

        var domainType = AppDomain.CurrentDomain.GetType();
        var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        resolversField.SetValue(AppDomain.CurrentDomain, _domainResolvers);

        _domainResolvers = null;
        _moduleDirectory = null;
    }

    private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        foreach (var assembly in Directory.EnumerateFiles(_moduleDirectory, "*.dll"))
        {
            if (assemblyName == Path.GetFileNameWithoutExtension(assembly))
            {
                return Assembly.LoadFile(assembly);
            }
        }

        return null;
    }
}