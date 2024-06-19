#if NETCOREAPP
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.Helpers;

/// <summary>
///     Isolated addin dependency container
/// </summary>
internal sealed class AddinLoadContext : AssemblyLoadContext
{
    /// <summary>
    ///     Add-ins contexts storage
    /// </summary>
    private static readonly Dictionary<string, AddinLoadContext> DependenciesProviders = new(1);
    
    private readonly AssemblyDependencyResolver _resolver;

    private AddinLoadContext(Type type, string addinName) : base(addinName)
    {
        var addinLocation = type.Assembly.Location;
        _resolver = new AssemblyDependencyResolver(addinLocation);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is not null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    /// <summary>
    ///     Get or create a new isolated context for the type
    /// </summary>
    public static AddinLoadContext GetDependenciesProvider(Type type)
    {
        var addinRoot = Path.GetDirectoryName(type.Assembly.Location)!;
        var addinName = Path.GetFileName(addinRoot)!;
        if (DependenciesProviders.TryGetValue(addinRoot, out var provider)) return provider;

        provider = new AddinLoadContext(type, addinName);
        DependenciesProviders.Add(addinRoot, provider);

        return provider;
    }

    /// <summary>
    ///     Determines whether the type is the type not associated with the default context
    /// </summary>
    public static bool CheckAccess(Type type)
    {
        var currentContext = GetLoadContext(type.Assembly);
        return currentContext != Default;
    }

    /// <summary>
    ///     Create new instance in the separated context
    /// </summary>
    public object CreateInstance(Type type)
    {
        var assemblyLocation = type.Assembly.Location;
        var assembly = LoadFromAssemblyPath(assemblyLocation);
        return assembly.CreateInstance(type.FullName!)!;
    }

    /// <summary>
    ///     Execute method in the separated context
    /// </summary>
    /// <remarks>Reference types are not supported</remarks>
    public static T Invoke<T>(object instance, string methodName, params object[] args)
    {
        var instanceType = instance.GetType();
        var methodParameterTypes = args.Select(arg => arg.GetType()).ToArray();
        var method = instanceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, methodParameterTypes, null)!;
        return (T)method.Invoke(instance, args);
    }

    /// <summary>
    ///     Execute IExternalCommand in the separated context
    /// </summary>
    public static Result ExecuteCommand(object command, ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        const BindingFlags searchFlags = BindingFlags.Public | BindingFlags.Instance;
        var instanceType = command.GetType();

        Type[] methodParameterTypes =
        [
            typeof(ExternalCommandData),
            typeof(string).MakeByRefType(),
            typeof(ElementSet)
        ];

        object[] methodParameters =
        [
            commandData,
            message,
            elements
        ];

        var method = instanceType.GetMethod(nameof(IExternalCommand.Execute), searchFlags, null, methodParameterTypes, null)!;
        var result = (Result)method.Invoke(command, methodParameters)!;
        message = (string)methodParameters[1];
        return result;
    }
}
#endif