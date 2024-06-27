#if NETCOREAPP
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Autodesk.Revit.ApplicationServices;
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
    private const BindingFlags MethodSearchFlags = BindingFlags.Public | BindingFlags.Instance;

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
        // Assembly location used as context name and the unique provider key
        var addinRoot = Path.GetDirectoryName(type.Assembly.Location)!;
        if (DependenciesProviders.TryGetValue(addinRoot, out var provider)) return provider;

        var addinName = Path.GetFileName(addinRoot)!;
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
    ///     Execute ExternalApplication in the separated context
    /// </summary>
    public static Result Invoke(object instance, string methodName, UIControlledApplication application)
    {
        var instanceType = instance.GetType();
        var method = instanceType.GetMethod(methodName, MethodSearchFlags, null, [typeof(UIControlledApplication)], null)!;
        return (Result)method.Invoke(instance, [application])!;
    }

    /// <summary>
    ///     Execute ExternalDbApplication in the separated context
    /// </summary>
    public static ExternalDBApplicationResult Invoke(object instance, string methodName, ControlledApplication application)
    {
        var instanceType = instance.GetType();
        var method = instanceType.GetMethod(methodName, MethodSearchFlags, null, [typeof(ControlledApplication)], null)!;
        return (ExternalDBApplicationResult)method.Invoke(instance, [application])!;
    }

    /// <summary>
    ///     Execute ExternalCommandAvailability in the separated context
    /// </summary>
    public static bool Invoke(object instance, string methodName, UIApplication applicationData, CategorySet selectedCategories)
    {
        var instanceType = instance.GetType();

        Type[] methodParameterTypes =
        [
            typeof(UIApplication),
            typeof(CategorySet)
        ];

        object[] methodParameters =
        [
            applicationData,
            selectedCategories
        ];

        var method = instanceType.GetMethod(methodName, MethodSearchFlags, null, methodParameterTypes, null)!;
        return (bool)method.Invoke(instance, methodParameters)!;
    }

    /// <summary>
    ///     Execute IExternalCommand in the separated context
    /// </summary>
    public static Result Invoke(object command, ExternalCommandData commandData, ref string message, ElementSet elements)
    {
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

        var method = instanceType.GetMethod(nameof(IExternalCommand.Execute), MethodSearchFlags, null, methodParameterTypes, null)!;
        var result = (Result)method.Invoke(command, methodParameters)!;
        message = (string)methodParameters[1];
        return result;
    }
}
#endif