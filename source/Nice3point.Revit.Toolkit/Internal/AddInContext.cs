using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI;
#if NET
using System.Runtime.Loader;
#endif

namespace Nice3point.Revit.Toolkit.Internal;

/// <summary>
///     Provides the captured add-in of the handler assembly to an external event created outside a Revit callback.
/// </summary>
/// <remarks>
///     Revit binds an external event to the add-in on top of the add-in data stack of the creating thread.
///     The stack is thread-local and empty outside a Revit callback, where Revit binds the event to no add-in.
///     A native <c>AddInDataScope</c> pushes a copy of the captured add-in data onto the stack of the current thread for the creation of the event.
///     A shared toolkit binds the assemblies listed by the generated <c>AddInAssemblyAttribute</c> to the add-in of the declaring assembly.
///     A listed assembly is matched by name at its first external event.
///     The context never frees a published copy of the add-in data.
///     A Revit version without a member of the expected signature creates every event without a scope.
/// </remarks>
internal static unsafe class AddInContext
{
    private static readonly bool IsSupported;
    private static readonly bool IsShared;
    private static readonly int AddInDataSize;
    private static readonly int AddInDataScopeSize;
    private static readonly delegate*<void*> GetAddInDataStack;
    private static readonly delegate*<void*, bool> IsAddInDataStackEmpty;
    private static readonly delegate*<void*, void*> GetActiveAddInData;
    private static readonly delegate*<void*, void*, void*> EnterAddInDataScope;
    private static readonly delegate*<void*, void> ExitAddInDataScope;

    private const string AddInAssemblyAttributeName = "Nice3point.Revit.Toolkit.Generated.AddInAssemblyAttribute";

    private static readonly Assembly ToolkitAssembly = typeof(AddInContext).Assembly;
    private static readonly ConditionalWeakTable<Assembly, StrongBox<nint>> CapturedAddIns = new();
    private static readonly ConditionalWeakTable<Assembly, StrongBox<nint>>.CreateValueCallback CreateEmptyCapture = static _ => new StrongBox<nint>();
    private static readonly Lock ListedAddInAssembliesLock = new();
    private static readonly ListedAddInAssemblies ListedAddInAssemblies = new();

    static AddInContext()
    {
#if NET
        // Add-ins without dependency isolation share the toolkit loaded into the default AssemblyLoadContext.
        IsShared = AssemblyLoadContext.GetLoadContext(ToolkitAssembly) == AssemblyLoadContext.Default;
#else
        IsShared = true;
#endif

        try
        {
            var uiAssembly = typeof(UIApplication).Assembly;
            var foundationReference = uiAssembly.GetReferencedAssemblies().First(static reference => reference.Name == "RevitAPIFoundation");
            var uiModule = uiAssembly.ManifestModule;
            var foundationModule = Assembly.Load(foundationReference).ManifestModule;

            var addInDataType = uiModule.GetType("AddInData", throwOnError: true, ignoreCase: false)!;
            var addInDataScopeType = uiModule.GetType("AddInDataScope", throwOnError: true, ignoreCase: false)!;
            var addInDataStackType = foundationModule.GetType("ArrStack<AddInData>", throwOnError: true, ignoreCase: false)!;
            var addInDataPointerType = addInDataType.MakePointerType();
            var addInDataScopePointerType = addInDataScopeType.MakePointerType();

            GetAddInDataStack = (delegate*<void*>)GetGlobalFunction(foundationModule, "getAddInDataStack");
            IsAddInDataStackEmpty = (delegate*<void*, bool>)GetGlobalFunction(foundationModule, "ArrStack<AddInData>.empty", addInDataStackType.MakePointerType());
            GetActiveAddInData = (delegate*<void*, void*>)GetGlobalFunction(uiModule, "getActiveAddInData", addInDataPointerType);
            EnterAddInDataScope = (delegate*<void*, void*, void*>)GetGlobalFunction(uiModule, "AddInDataScope.{ctor}", addInDataScopePointerType, addInDataPointerType);
            ExitAddInDataScope = (delegate*<void*, void>)GetGlobalFunction(uiModule, "AddInDataScope.{dtor}", addInDataScopePointerType);

            // The native sizes differ between Revit versions.
            AddInDataSize = addInDataType.StructLayoutAttribute?.Size ?? 0;
            AddInDataScopeSize = addInDataScopeType.StructLayoutAttribute?.Size ?? 0;
            IsSupported = AddInDataSize > 0 && AddInDataScopeSize > 0;
        }
        catch (Exception)
        {
            IsSupported = false;
        }
    }

    /// <summary>
    ///     Captures the add-in of the current Revit callback as the add-in of the assembly.
    /// </summary>
    /// <param name="assembly">The assembly that declares the callback.</param>
    /// <remarks>
    ///     A call for an assembly with a captured add-in returns immediately.
    ///     Outside a Revit callback, the call captures nothing.
    /// </remarks>
    internal static void Capture(Assembly assembly)
    {
        if (!IsSupported)
        {
            return;
        }

        CaptureAddIn(IsShared ? assembly : ToolkitAssembly);
    }

    /// <summary>
    ///     Captures the add-in of the current Revit callback for the assembly, replacing an earlier capture.
    /// </summary>
    /// <param name="assembly">The assembly to capture the add-in for.</param>
    /// <exception cref="T:System.InvalidOperationException">The current thread runs outside a Revit callback.</exception>
    /// <remarks>
    ///     The assemblies listed by the generated <c>AddInAssemblyAttribute</c> receive the same add-in.
    /// </remarks>
    internal static void Register(Assembly assembly)
    {
        if (!IsSupported)
        {
            return;
        }

        if (!IsAddInActive())
        {
            throw new InvalidOperationException("No add-in is active on the current thread. Call the method in OnStartup or in the Execute method of a command.");
        }

        var owner = IsShared ? assembly : ToolkitAssembly;
        var addInData = CopyActiveAddInData();

        // A concurrent event can read the replaced copy, and the context never frees it.
        var capturedAddIn = CapturedAddIns.GetValue(owner, CreateEmptyCapture);
        Volatile.Write(ref capturedAddIn.Value, addInData);

        ListAddInAssemblies(owner, addInData, replaceListedNames: true);
    }

    /// <summary>
    ///     Creates the Revit external event of the event handler, bound to the add-in of the handler assembly.
    /// </summary>
    /// <param name="eventHandler">The external event handler.</param>
    /// <param name="handler">The delegate that implements the execution logic, or <see langword="null" /> when the type of <paramref name="eventHandler" /> implements it.</param>
    /// <returns>The Revit external event.</returns>
    /// <remarks>
    ///     The handler assembly is the assembly of <paramref name="handler" />, or of the type of <paramref name="eventHandler" />.
    ///     Inside a Revit callback, Revit binds the event to the active add-in.
    ///     The call captures the active add-in for the handler assembly.
    ///     Outside a Revit callback, the call binds the event to the captured add-in of the handler assembly.
    /// </remarks>
    internal static ExternalEvent CreateExternalEvent(IExternalEventHandler eventHandler, Delegate? handler)
    {
        if (!IsSupported)
        {
            return ExternalEvent.Create(eventHandler);
        }

        var owner = FindOwner(eventHandler, handler);
        if (IsAddInActive())
        {
            CaptureAddIn(owner);
            return ExternalEvent.Create(eventHandler);
        }

        if (!TryGetCapturedAddIn(owner, out var addInData))
        {
            return ExternalEvent.Create(eventHandler);
        }

        // The native scope stores its state in the buffer, and the destructor reads it back.
        var scope = stackalloc byte[AddInDataScopeSize];
        EnterAddInDataScope(scope, (void*)addInData);
        try
        {
            return ExternalEvent.Create(eventHandler);
        }
        finally
        {
            ExitAddInDataScope(scope);
        }
    }

    /// <summary>
    ///     Captures the active add-in for the assembly without a captured add-in.
    /// </summary>
    /// <param name="owner">The assembly to store the add-in under.</param>
    /// <remarks>
    ///     The first copy published by concurrent captures wins.
    ///     The method frees a copy that loses.
    /// </remarks>
    private static void CaptureAddIn(Assembly owner)
    {
        if (CapturedAddIns.TryGetValue(owner, out var capturedAddIn) && Volatile.Read(ref capturedAddIn.Value) != IntPtr.Zero)
        {
            return;
        }

        if (!IsAddInActive())
        {
            return;
        }

        capturedAddIn = CapturedAddIns.GetValue(owner, CreateEmptyCapture);
        var addInData = CopyActiveAddInData();
        if (Interlocked.CompareExchange(ref capturedAddIn.Value, addInData, IntPtr.Zero) != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(addInData);
            return;
        }

        ListAddInAssemblies(owner, addInData, replaceListedNames: false);
    }

    /// <summary>
    ///     Copies the active add-in data into a new native buffer.
    /// </summary>
    /// <returns>The pointer to the copy.</returns>
    private static nint CopyActiveAddInData()
    {
        var addInData = Marshal.AllocHGlobal(AddInDataSize);
        GetActiveAddInData((void*)addInData);
        return addInData;
    }

    /// <summary>
    ///     Lists the assemblies named by the generated <c>AddInAssemblyAttribute</c> of the assembly under its add-in.
    /// </summary>
    /// <param name="owner">The assembly that declares the attributes.</param>
    /// <param name="addInData">The pointer to the add-in data of the assembly.</param>
    /// <param name="replaceListedNames"><see langword="true" /> to overwrite a name listed by another assembly; otherwise, <see langword="false" />.</param>
    /// <remarks>
    ///     An isolated toolkit lists no names.
    /// </remarks>
    private static void ListAddInAssemblies(Assembly owner, nint addInData, bool replaceListedNames)
    {
        if (!IsShared || !IsInDefaultContext(owner))
        {
            return;
        }

        var assemblyNames = ReadListedAssemblyNames(owner);
        if (assemblyNames.Length == 0)
        {
            return;
        }

        lock (ListedAddInAssembliesLock)
        {
            ListedAddInAssemblies.Add(assemblyNames, addInData, replaceListedNames);
        }
    }

    /// <summary>
    ///     Reads the assembly names listed by the generated <c>AddInAssemblyAttribute</c> of the assembly.
    /// </summary>
    /// <param name="assembly">The assembly to read the attributes of.</param>
    /// <returns>The listed assembly names, or an empty array when the attributes can't be read.</returns>
    /// <remarks>
    ///     The source generator declares the attribute type in each assembly, and the method matches it by full name.
    ///     An attribute type from a missing assembly fails the read.
    ///     A failed read lists no names: the capture never fails the startup of an add-in.
    /// </remarks>
    private static string[] ReadListedAssemblyNames(Assembly assembly)
    {
        try
        {
            return assembly.GetCustomAttributesData()
                .Where(static attribute => attribute.AttributeType.FullName == AddInAssemblyAttributeName)
                .Select(static attribute => attribute.ConstructorArguments[0].Value)
                .OfType<string>()
                .ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    ///     Determines whether the assembly is loaded into the default <c>AssemblyLoadContext</c>.
    /// </summary>
    /// <param name="assembly">The assembly to check.</param>
    /// <returns><see langword="true" /> if the assembly is loaded into the default context; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     A shared toolkit matches names for the default context only.
    ///     An isolated add-in can load its own assembly with a listed name.
    /// </remarks>
    private static bool IsInDefaultContext(Assembly assembly)
    {
#if NET
        return AssemblyLoadContext.GetLoadContext(assembly) == AssemblyLoadContext.Default;
#else
        return true;
#endif
    }

    /// <summary>
    ///     Gets the captured add-in data of the assembly.
    /// </summary>
    /// <param name="owner">The assembly the captured add-in is stored under.</param>
    /// <param name="addInData">When this method returns, contains the pointer to the captured add-in data, if found.</param>
    /// <returns><see langword="true" /> if the assembly has a captured add-in; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     On a miss, the method looks up the assembly name among the listed names.
    ///     A match becomes the capture of the assembly, unless a concurrent capture publishes first.
    /// </remarks>
    private static bool TryGetCapturedAddIn(Assembly owner, out nint addInData)
    {
        if (CapturedAddIns.TryGetValue(owner, out var capturedAddIn))
        {
            addInData = Volatile.Read(ref capturedAddIn.Value);
            if (addInData != 0)
            {
                return true;
            }
        }

        addInData = 0;
        if (!IsShared || !IsInDefaultContext(owner))
        {
            return false;
        }

        nint listedAddInData;
        lock (ListedAddInAssembliesLock)
        {
            if (!ListedAddInAssemblies.TryMatch(owner, out listedAddInData))
            {
                return false;
            }
        }

        capturedAddIn = CapturedAddIns.GetValue(owner, CreateEmptyCapture);
        Interlocked.CompareExchange(ref capturedAddIn.Value, listedAddInData, IntPtr.Zero);
        addInData = Volatile.Read(ref capturedAddIn.Value);
        return true;
    }

    /// <summary>
    ///     Determines whether the add-in data stack of the current thread contains an add-in.
    /// </summary>
    /// <returns><see langword="true" /> if the current thread runs inside a Revit callback of an add-in; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     Revit registers an external event for an add-in only when the add-in data stack contains one.
    ///     The active add-in data does not indicate an empty stack: it contains the null GUID before Revit 2027 and a fixed non-null GUID since.
    /// </remarks>
    private static bool IsAddInActive()
    {
        return !IsAddInDataStackEmpty(GetAddInDataStack());
    }

    /// <summary>
    ///     Finds the assembly the captured add-in of the event handler is stored under.
    /// </summary>
    /// <param name="eventHandler">The external event handler.</param>
    /// <param name="handler">The delegate that implements the execution logic, or <see langword="null" /> when the type of <paramref name="eventHandler" /> implements it.</param>
    /// <returns>The handler assembly for a shared toolkit, or the toolkit assembly for an isolated toolkit.</returns>
    /// <remarks>
    ///     An isolated toolkit stores one add-in for every assembly under its own assembly.
    ///     The delegate target is the closure, the lambda cache, or the instance of the consumer code.
    ///     The target comes first: <see cref="Delegate.Method" /> runs a reflection lookup and allocates on every new delegate instance.
    /// </remarks>
    private static Assembly FindOwner(IExternalEventHandler eventHandler, Delegate? handler)
    {
        if (!IsShared)
        {
            return ToolkitAssembly;
        }

        if (handler is null)
        {
            return eventHandler.GetType().Assembly;
        }

        return handler.Target?.GetType().Assembly ?? handler.Method.Module.Assembly;
    }

    private static nint GetGlobalFunction(Module module, string name, params Type[] parameterTypes)
    {
        var method = module.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static, binder: null, CallingConventions.Any, parameterTypes, modifiers: null);
        if (method is null)
        {
            throw new MissingMethodException(module.Name, name);
        }

        // The function pointer of a global method is the managed entry of its native call stub.
        return method.MethodHandle.GetFunctionPointer();
    }
}
