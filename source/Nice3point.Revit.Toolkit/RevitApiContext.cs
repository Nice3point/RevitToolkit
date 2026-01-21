#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Nice3point.Revit.Toolkit.Utils;

namespace Nice3point.Revit.Toolkit;

/// <summary>
///     Provides members for accessing the Revit application context at the database level.
/// </summary>
[PublicAPI]
public class RevitApiContext
{
    //Global state
    private static readonly Lock FailureLock = new();
    private static bool _suppressFailures;
    private static bool _suppressFailureErrors;

    static RevitApiContext()
    {
        var dbAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "RevitDBAPI");
        ThrowWhen(dbAssembly is null);
        
        var dbAssemblyMethods = dbAssembly.ManifestModule.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
        var getApplicationMethod = dbAssemblyMethods.FirstOrDefault(info => info.Name == "RevitApplication.getApplication_");
        ThrowWhen(getApplicationMethod is null);

        var proxyType = dbAssembly.DefinedTypes.FirstOrDefault(info => info.FullName == "Autodesk.Revit.Proxy.ApplicationServices.ApplicationProxy");
        ThrowWhen(proxyType is null);

        const BindingFlags internalFlags = BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
        var proxyConstructor = proxyType.GetConstructor(internalFlags, null, [getApplicationMethod.ReturnType], null);
        ThrowWhen(proxyConstructor is null);

        var proxy = proxyConstructor.Invoke([getApplicationMethod.Invoke(null, null)]);
        ThrowWhen(proxy is null);

#if NET8_0_OR_GREATER
        Application = CreateApplication(proxy);
#else
        var applicationType = typeof(Application);
        var applicationConstructor = applicationType.GetConstructor(internalFlags, null, [proxyType], null);
        ThrowWhen(applicationConstructor is null);

        var application = (Application)applicationConstructor.Invoke([proxy]);
        ThrowWhen(application is null);

        Application = application;
#endif
    }

    /// <summary>
    ///     Represents the database level Autodesk Revit Application, providing access to documents, options and other application wide data and settings.
    /// </summary>
    public static Application Application { get; }

    /// <summary>
    ///     Suppresses the display of the Revit error and warning messages during transaction.
    /// </summary>
    /// <param name="resolveErrors">
    ///     Set <see langword="true"/> if errors should be automatically resolved, otherwise <see langword="false"/> to cancel the transaction.
    /// </param>
    /// <remarks>
    ///     By default, Revit uses manual error resolution control with user interaction.
    ///     This method provides automatic resolution of all failures without notifying the user or interrupting the program.
    ///     This method is thread-safe.
    /// </remarks>
    public static void SuppressFailures(bool resolveErrors = true)
    {
        lock (FailureLock)
        {
            if (_suppressFailures)
            {
                _suppressFailureErrors = resolveErrors;
                return;
            }

            _suppressFailures = true;
            _suppressFailureErrors = resolveErrors;
            Application.FailuresProcessing += ResolveFailures;
        }
    }

    /// <summary>
    ///     Restores failure handling.
    /// </summary>
    /// <remarks>
    ///     This method is thread-safe.
    /// </remarks>
    public static void RestoreFailures()
    {
        lock (FailureLock)
        {
            _suppressFailures = false;
            Application.FailuresProcessing -= ResolveFailures;
        }
    }

    private static void ResolveFailures(object? sender, FailuresProcessingEventArgs args)
    {
        var failuresAccessor = args.GetFailuresAccessor();
        var result = _suppressFailureErrors ? FailureUtils.ResolveFailures(failuresAccessor) : FailureUtils.DismissFailures(failuresAccessor);

        args.SetProcessingResult(result);
    }

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern Application CreateApplication(object proxy);
#endif

    /// <summary>
    ///     Dynamically throw when the <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    private protected static void ThrowWhen([DoesNotReturnIf(true)] bool condition)
    {
        if (condition)
        {
            throw new NotSupportedException("The operation is not supported by current Revit API version. Failed to retrieve the application context.");
        }
    }
}