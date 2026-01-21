using System.IO;
using System.Reflection;
#if NET
using System.Runtime.Loader;
#endif

namespace Nice3point.Revit.Toolkit.Helpers;

/// <summary>
///     Provides methods to resolve dependencies.
/// </summary>
/// <example>
///     <code lang="csharp">
/// // Recommended: Use scope-based pattern
/// using (ResolveHelper.BeginAssemblyResolveScope&lt;MyType&gt;())
/// {
///     return (T)Activator.CreateInstance(typeof(T));
/// }
/// </code>
/// </example>
[PublicAPI]
public static class ResolveHelper
{
    private static string? _moduleDirectory;
    private static object? _domainResolvers;

    /// <summary>
    ///     Begins a scope that resolves dependencies for the specified type.
    ///     Assembly resolution is automatically ended when the returned scope is disposed.
    /// </summary>
    /// <typeparam name="T">Type, to search for dependencies in the directory where this type is defined.</typeparam>
    /// <returns>A disposable scope. Call Dispose or use 'using' statement to end assembly resolution.</returns>
    /// <remarks>
    ///     Dependencies are searched in a directory of the specified type.
    ///     At the time of dependency resolution, all other dependency resolution methods for the domain are set to low priority,
    ///     this requires calling Dispose immediately after executing user code to avoid conflict with Revit runtime.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         using (ResolveHelper.BeginAssemblyResolveScope&lt;MyViewModel&gt;())
    ///         {
    ///             return new MyWindow();
    ///         }
    ///     </code>
    /// </example>
    public static IDisposable BeginAssemblyResolveScope<T>()
    {
        return BeginAssemblyResolveScope(typeof(T));
    }

    /// <summary>
    ///     Begins a scope that resolves dependencies for the specified type module.
    ///     Assembly resolution is automatically ended when the returned scope is disposed.
    /// </summary>
    /// <param name="type">Type, to search for dependencies in the directory where this type is defined.</param>
    /// <returns>A disposable scope. Call Dispose or use 'using' statement to end assembly resolution.</returns>
    /// <remarks>
    ///     Dependencies are searched in a directory of the specified type.
    ///     At the time of dependency resolution, all other dependency resolution methods for the domain are set to low priority,
    ///     this requires calling Dispose immediately after executing user code to avoid conflict with Revit runtime.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         using (ResolveHelper.BeginAssemblyResolveScope(typeof(MyViewModel)))
    ///         {
    ///             return new MyWindow();
    ///         }
    ///     </code>
    /// </example>
    public static IDisposable BeginAssemblyResolveScope(Type type)
    {
        if (_domainResolvers is not null) return new AssemblyResolveScope();
        if (type.Module.FullyQualifiedName == "<Unknown>") return new AssemblyResolveScope();

#if NET
        var loadContextType = typeof(AssemblyLoadContext);
        var resolversField = loadContextType.GetField("AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
        var resolvers = (ResolveEventHandler?)resolversField.GetValue(null);
        resolversField.SetValue(null, null);
#else
        var domainType = AppDomain.CurrentDomain.GetType();
        var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        var resolvers = (ResolveEventHandler)resolversField.GetValue(AppDomain.CurrentDomain);
        resolversField.SetValue(AppDomain.CurrentDomain, null);
#endif

        _domainResolvers = resolvers;
        _moduleDirectory = Path.GetDirectoryName(type.Module.FullyQualifiedName);

        // Set priority on the add-in's resolver
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        AppDomain.CurrentDomain.AssemblyResolve += resolvers;

        return new AssemblyResolveScope();
    }

    /// <summary>
    ///     Subscribes the current domain to resolve dependencies for the type.
    /// </summary>
    /// <typeparam name="T">Type, to search for dependencies in the directory where this type is defined.</typeparam>
    /// <remarks>
    ///     Dependencies are searched in a directory of the specified type.
    ///     At the time of dependency resolution, all other dependency resolution methods for the domain are disabled,
    ///     this requires calling <see cref="EndAssemblyResolve" /> immediately after executing user code where dependency failures occur.
    /// </remarks>
    [Obsolete("Use BeginAssemblyResolveScope<T> instead for automatic resource management")]
    [CodeTemplate(
        searchTemplate: "BeginAssemblyResolve<$T$>()",
        Message = "BeginAssemblyResolve is obsolete. Use BeginAssemblyResolveScope with 'using' statement instead",
        ReplaceTemplate = "BeginAssemblyResolveScope<$T$>()",
        ReplaceMessage = "Replace with BeginAssemblyResolveScope")]
    public static void BeginAssemblyResolve<T>()
    {
        BeginAssemblyResolveScope(typeof(T));
    }

    /// <summary>
    ///     Subscribes the current domain to resolve dependencies for the type module.
    /// </summary>
    /// <param name="type">Type, to search for dependencies in the directory where this type is defined.</param>
    /// <remarks>
    ///     Dependencies are searched in a directory of the specified type.
    ///     At the time of dependency resolution, all other dependency resolution methods for the domain are set to low priority,
    ///     this requires calling <see cref="EndAssemblyResolve" /> immediately after executing user code to avoid conflict with Revit runtime.
    /// </remarks>
    [Obsolete("Use BeginAssemblyResolveScope instead for automatic resource management")]
    [CodeTemplate(
        searchTemplate: "BeginAssemblyResolve($type$)",
        Message = "BeginAssemblyResolve is obsolete. Use BeginAssemblyResolveScope with 'using' statement instead",
        ReplaceTemplate = "BeginAssemblyResolveScope($type$)",
        ReplaceMessage = "Replace with BeginAssemblyResolveScope")]
    public static void BeginAssemblyResolve(Type type)
    {
        BeginAssemblyResolveScope(type);
    }

    /// <summary>
    ///     Unsubscribes the current domain to resolve dependencies for the type module.
    /// </summary>
    [Obsolete("Use BeginAssemblyResolveScope instead for automatic resource management")]
    public static void EndAssemblyResolve()
    {
        if (_domainResolvers is null) return;

#if NET
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

    private sealed class AssemblyResolveScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_domainResolvers is null) return;

#if NET
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
    }
}