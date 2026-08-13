using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Nice3point.Revit.Toolkit.Utils;
#if NET8_0_OR_GREATER
using Nice3point.Revit.Toolkit.Internal;
#endif

namespace Nice3point.Revit.Toolkit;

/// <summary>
///     Provides members for accessing the Revit application context at the database level.
/// </summary>
[PublicAPI]
public class RevitApiContext
{
    private static readonly Lock FailureLock = new();

    private static int _failureScopeCount;
    private static bool _suppressFailureErrors;

    static RevitApiContext()
    {
        var assemblies = FindAssemblies("RevitDBAPI");

        var dbAssemblyMethods = assemblies[0].ManifestModule.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
        var getApplicationMethod = dbAssemblyMethods.FirstOrDefault(static info => info.Name == "RevitApplication.getApplication_");
        ThrowWhen(getApplicationMethod is null);

        var proxyType = assemblies[0].DefinedTypes.FirstOrDefault(static info => info.FullName == "Autodesk.Revit.Proxy.ApplicationServices.ApplicationProxy");
        ThrowWhen(proxyType is null);

        const BindingFlags internalFlags = BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
        var proxyConstructor = proxyType.GetConstructor(internalFlags, null, [getApplicationMethod.ReturnType], null);
        ThrowWhen(proxyConstructor is null);

        var proxy = proxyConstructor.Invoke([getApplicationMethod.Invoke(null, null)]);
        ThrowWhen(proxy is null);

#if NET8_0_OR_GREATER
        Application = UnsafeAccessors.CreateApplication(proxy);
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
    ///     Begins a scope that suppresses the display of Revit error and warning messages during transaction.
    ///     Failure handling is automatically restored when the returned scope is disposed.
    /// </summary>
    /// <param name="resolveErrors">
    ///     Set <see langword="true" /> if errors should be automatically resolved, otherwise <see langword="false" /> to cancel the transaction.
    /// </param>
    /// <returns>A disposable scope. Call Dispose or use 'using' statement to restore failure handling.</returns>
    /// <remarks>
    ///     By default, Revit uses manual error resolution control with user interaction.
    ///     This method provides automatic resolution of all failures without notifying the user or interrupting the program.
    ///     This method is thread-safe.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         using (RevitApiContext.BeginFailureSuppressionScope())
    ///         {
    ///             using var transaction = new Transaction(document, "Operation");
    ///             transaction.Start();
    ///             // Operations that may cause failures
    ///             transaction.Commit();
    ///         }
    ///         // Failure handling is restored automatically
    ///     </code>
    /// </example>
    public static IDisposable BeginFailureSuppressionScope(bool resolveErrors = true)
    {
        lock (FailureLock)
        {
            _suppressFailureErrors = resolveErrors;

            if (_failureScopeCount++ == 0)
            {
                Application.FailuresProcessing += ResolveFailures;
            }
        }

        return new FailureSuppressionScope();
    }

    /// <summary>
    ///     Finds assemblies loaded in the current application domain that match the specified names.
    /// </summary>
    protected static Assembly[] FindAssemblies(params string[] names)
    {
        var remaining = new HashSet<string>(names);
        var result = new Dictionary<string, Assembly>(names.Length);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name is not null && remaining.Remove(name))
            {
                result[name] = assembly;
                if (remaining.Count == 0)
                {
                    break;
                }
            }
        }

        ThrowWhen(remaining.Count > 0);
        return names.Select(name => result[name]).ToArray();
    }

    private static void ResolveFailures(object? sender, FailuresProcessingEventArgs args)
    {
        bool resolveErrors;
        lock (FailureLock)
        {
            resolveErrors = _suppressFailureErrors;
        }

        var failuresAccessor = args.GetFailuresAccessor();
        var result = resolveErrors
            ? FailureUtils.ResolveFailures(failuresAccessor)
            : FailureUtils.DismissFailures(failuresAccessor);

        args.SetProcessingResult(result);
    }

    /// <summary>
    ///     Dynamically throw when the <paramref name="condition" /> is <c>true</c>.
    /// </summary>
    private protected static void ThrowWhen([DoesNotReturnIf(true)] bool condition)
    {
        if (condition)
        {
            throw new NotSupportedException("The operation is not supported by current Revit API version. Failed to retrieve the application context.");
        }
    }

    private sealed class FailureSuppressionScope : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            lock (FailureLock)
            {
                if (--_failureScopeCount == 0)
                {
                    Application.FailuresProcessing -= ResolveFailures;
                }
            }
        }
    }
}
