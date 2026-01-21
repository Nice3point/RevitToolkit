#if NET
using System.Runtime.Loader;
#endif
using System.Windows;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.Helpers;

namespace Nice3point.Revit.Toolkit.Options;

/// <summary>
///     Class that the Revit UI will call, if present, to construct the FrameworkElement for the pane.
/// </summary>
[PublicAPI]
public class FrameworkElementCreator<T> : IFrameworkElementCreator where T : FrameworkElement
{
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    ///     Factory to create an instance of a FrameworkElement based on its type.
    /// </summary>
    public FrameworkElementCreator()
    {
    }

    /// <summary>
    ///     Factory to create an instance of a FrameworkElement based on its type using IServiceProvider.
    /// </summary>
    /// <param name="serviceProvider">Instance of the <see cref="IServiceProvider" />.</param>
    public FrameworkElementCreator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    ///     Method called to create the FrameworkElement.
    /// </summary>
    /// <returns>Created FrameworkElement.</returns>
    public FrameworkElement? CreateFrameworkElement()
    {
        var elementType = typeof(T);

        try
        {
            var currentType = GetType();
#if NET
            if (AssemblyLoadContext.GetLoadContext(currentType.Assembly) == AssemblyLoadContext.Default)
            {
                ResolveHelper.BeginAssemblyResolve(currentType);
            }
#else
            ResolveHelper.BeginAssemblyResolve(currentType);
#endif
            return CreateInstance(elementType);
        }
        finally
        {
            ResolveHelper.EndAssemblyResolve();
        }
    }

    private FrameworkElement? CreateInstance(Type elementType)
    {
        if (_serviceProvider is not null)
        {
            return (FrameworkElement?)_serviceProvider.GetService(elementType);
        }

        return (FrameworkElement?)Activator.CreateInstance(elementType);
    }
}