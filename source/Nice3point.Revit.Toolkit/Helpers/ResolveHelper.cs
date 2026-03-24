using System.IO;
using System.Reflection;
using JetBrains.Annotations;
#if NET8_0_OR_GREATER
using Nice3point.Revit.Toolkit.Internal;
#endif
#if !NET8_0_OR_GREATER && NET
using System.Runtime.Loader;
#endif

namespace Nice3point.Revit.Toolkit.Helpers;

/// <summary>
///     Provides methods to resolve dependencies.
/// </summary>
[PublicAPI]
public static class ResolveHelper
{
    private static readonly Lock ResolveLock = new();
    private static readonly Stack<string> ModuleDirectories = new();
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
        if (type.Module.FullyQualifiedName == "<Unknown>")
        {
            return DisposedAssemblyResolveScope.Instance;
        }

        var moduleDirectory = Path.GetDirectoryName(type.Module.FullyQualifiedName);
        if (moduleDirectory is null)
        {
            return DisposedAssemblyResolveScope.Instance;
        }

        return BeginAssemblyResolveScope(moduleDirectory);
    }

    /// <summary>
    ///     Begins a scope that resolves dependencies from the specified directory.
    ///     Assembly resolution is automatically ended when the returned scope is disposed.
    /// </summary>
    /// <param name="directory">The directory path to search for dependencies.</param>
    /// <returns>A disposable scope. Call Dispose or use 'using' statement to end assembly resolution.</returns>
    /// <remarks>
    ///     At the time of dependency resolution, all other dependency resolution methods for the domain are set to low priority,
    ///     this requires calling Dispose immediately after executing user code to avoid conflict with Revit runtime.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         using (ResolveHelper.BeginAssemblyResolveScope(@"C:\Libraries"))
    ///         {
    ///             return new MyWindow();
    ///         }
    ///     </code>
    /// </example>
    public static IDisposable BeginAssemblyResolveScope(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return DisposedAssemblyResolveScope.Instance;
        }

        lock (ResolveLock)
        {
            var isFirstScope = ModuleDirectories.Count == 0;
            ModuleDirectories.Push(directory);

            if (isFirstScope)
            {
                OverrideDomainResolvers();
            }
        }

        return new AssemblyResolveScope();
    }

    private static void OverrideDomainResolvers()
    {
#if NET8_0_OR_GREATER
        ref var resolversRef = ref UnsafeDomainAccessors.GetAssemblyResolveField(null!);
        var resolvers = resolversRef;
        resolversRef = null;
#elif NET
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

        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        AppDomain.CurrentDomain.AssemblyResolve += resolvers;
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
        lock (ResolveLock)
        {
            if (ModuleDirectories.Count == 0) return;
            ModuleDirectories.Pop();

            if (ModuleDirectories.Count == 0)
            {
                RestoreResolvers();
            }
        }
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        string[] directories;
        lock (ResolveLock)
        {
            if (ModuleDirectories.Count == 0) return null;
            directories = ModuleDirectories.ToArray();
        }

        var assemblyName = new AssemblyName(args.Name).Name;

        // Search from innermost scope to outermost
        foreach (var directory in directories)
        {
            var assemblyPath = Path.Combine(directory, $"{assemblyName}.dll");
            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }
        }

        return null;
    }

    private static void RestoreResolvers()
    {
        if (_domainResolvers is null) return;

        AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;

#if NET8_0_OR_GREATER
        UnsafeDomainAccessors.GetAssemblyResolveField(null!) = (ResolveEventHandler?)_domainResolvers;
#elif NET
        var loadContextType = typeof(AssemblyLoadContext);
        var resolversField = loadContextType.GetField("AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
        resolversField.SetValue(null, _domainResolvers);
#else
        var domainType = AppDomain.CurrentDomain.GetType();
        var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        resolversField.SetValue(AppDomain.CurrentDomain, _domainResolvers);
#endif

        _domainResolvers = null;
    }

    private sealed class AssemblyResolveScope : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            lock (ResolveLock)
            {
                if (ModuleDirectories.Count == 0) return;
                ModuleDirectories.Pop();

                if (ModuleDirectories.Count == 0)
                {
                    RestoreResolvers();
                }
            }
        }
    }

    private sealed class DisposedAssemblyResolveScope : IDisposable
    {
        public static readonly DisposedAssemblyResolveScope Instance = new();

        private DisposedAssemblyResolveScope()
        {
        }

        public void Dispose()
        {
        }
    }
}