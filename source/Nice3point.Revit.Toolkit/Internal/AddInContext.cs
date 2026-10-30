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
///     The context never frees a captured copy of the add-in data.
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

    private static readonly Assembly ToolkitAssembly = typeof(AddInContext).Assembly;
    private static readonly Lock CaptureLock = new();
    private static readonly ConditionalWeakTable<Assembly, StrongBox<nint>> CapturedAddIns = new();

    static AddInContext()
    {
        // A toolkit in the AssemblyLoadContext of an isolated add-in runs for that add-in alone.
#if NET
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
    /// <param name="assembly">The assembly whose code the callback runs.</param>
    /// <remarks>
    ///     A later call leaves the captured add-in of the assembly unchanged.
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

        if (!CapturedAddIns.TryGetValue(owner, out var capturedAddIn))
        {
            return ExternalEvent.Create(eventHandler);
        }

        // The native scope stores its state in the buffer, and the destructor reads it back.
        var scope = stackalloc byte[AddInDataScopeSize];
        EnterAddInDataScope(scope, (void*)capturedAddIn.Value);
        try
        {
            return ExternalEvent.Create(eventHandler);
        }
        finally
        {
            ExitAddInDataScope(scope);
        }
    }

    private static void CaptureAddIn(Assembly owner)
    {
        if (CapturedAddIns.TryGetValue(owner, out _))
        {
            return;
        }

        if (!IsAddInActive())
        {
            return;
        }

        lock (CaptureLock)
        {
            if (CapturedAddIns.TryGetValue(owner, out _))
            {
                return;
            }

            var addInData = Marshal.AllocHGlobal(AddInDataSize);
            GetActiveAddInData((void*)addInData);
            CapturedAddIns.Add(owner, new StrongBox<nint>(addInData));
        }
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
