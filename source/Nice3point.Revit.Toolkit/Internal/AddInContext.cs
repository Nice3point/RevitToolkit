using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI;
#if NET
using System.Runtime.Loader;
#endif

namespace Nice3point.Revit.Toolkit.Internal;

/// <summary>
///     Binds an external event created outside a Revit callback to the add-in captured for its handler.
/// </summary>
/// <remarks>
///     Revit binds an external event to the add-in on top of the add-in data stack of the creating thread.
///     The stack is thread-local and empty outside a Revit callback, where Revit binds the event to no add-in.
///     A native <c>AddInDataScope</c> pushes a copy of the captured add-in data onto the stack of the current thread for the creation of the event.
///     A captured copy of the add-in data lives as long as the process.
///     The binding turns off when the Revit version exposes no member of the expected signature.
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

    private static readonly Lock CaptureLock = new();
    private static readonly ConditionalWeakTable<Assembly, CapturedAddIn> SharedAddIns = new();
    private static IntPtr _isolatedAddInData;

    static AddInContext()
    {
        var uiAssembly = typeof(UIApplication).Assembly;
        var foundationReference = uiAssembly.GetReferencedAssemblies().FirstOrDefault(static reference => reference.Name == "RevitAPIFoundation");
        if (foundationReference is null)
        {
            return;
        }

        var uiModule = uiAssembly.ManifestModule;
        var foundationModule = Assembly.Load(foundationReference).ManifestModule;
        var addInDataType = uiModule.GetType("AddInData");
        var addInDataScopeType = uiModule.GetType("AddInDataScope");
        var addInDataStackType = foundationModule.GetType("ArrStack<AddInData>");
        if (addInDataType is null || addInDataScopeType is null || addInDataStackType is null)
        {
            return;
        }

        var addInDataPointerType = addInDataType.MakePointerType();
        var addInDataScopePointerType = addInDataScopeType.MakePointerType();
        var getAddInDataStack = FindGlobalMethod(foundationModule, "getAddInDataStack");
        var isAddInDataStackEmpty = FindGlobalMethod(foundationModule, "ArrStack<AddInData>.empty", addInDataStackType.MakePointerType());
        var getActiveAddInData = FindGlobalMethod(uiModule, "getActiveAddInData", addInDataPointerType);
        var enterAddInDataScope = FindGlobalMethod(uiModule, "AddInDataScope.{ctor}", addInDataScopePointerType, addInDataPointerType);
        var exitAddInDataScope = FindGlobalMethod(uiModule, "AddInDataScope.{dtor}", addInDataScopePointerType);
        if (getAddInDataStack is null || isAddInDataStackEmpty is null || getActiveAddInData is null || enterAddInDataScope is null || exitAddInDataScope is null)
        {
            return;
        }

        // The function pointer of a global method is the managed entry of its native call stub.
        GetAddInDataStack = (delegate*<void*>)getAddInDataStack.MethodHandle.GetFunctionPointer();
        IsAddInDataStackEmpty = (delegate*<void*, bool>)isAddInDataStackEmpty.MethodHandle.GetFunctionPointer();
        GetActiveAddInData = (delegate*<void*, void*>)getActiveAddInData.MethodHandle.GetFunctionPointer();
        EnterAddInDataScope = (delegate*<void*, void*, void*>)enterAddInDataScope.MethodHandle.GetFunctionPointer();
        ExitAddInDataScope = (delegate*<void*, void>)exitAddInDataScope.MethodHandle.GetFunctionPointer();

        // The native sizes differ between Revit versions.
        AddInDataSize = addInDataType.StructLayoutAttribute?.Size ?? 0;
        AddInDataScopeSize = addInDataScopeType.StructLayoutAttribute?.Size ?? 0;

        // A toolkit in the AssemblyLoadContext of an isolated add-in runs for that add-in alone.
#if NET
        IsShared = AssemblyLoadContext.GetLoadContext(typeof(AddInContext).Assembly) == AssemblyLoadContext.Default;
#else
        IsShared = true;
#endif
        IsSupported = AddInDataSize > 0 && AddInDataScopeSize > 0;
    }

    /// <summary>
    ///     Captures the add-in of the current Revit callback as the add-in of the assembly.
    /// </summary>
    /// <param name="assembly">The assembly whose code the callback runs.</param>
    /// <remarks>
    ///     The first captured add-in of an assembly stays.
    ///     The call captures nothing outside a Revit callback.
    /// </remarks>
    internal static void Capture(Assembly assembly)
    {
        if (!IsSupported)
        {
            return;
        }

        CaptureOwner(IsShared ? assembly : null);
    }

    /// <summary>
    ///     Creates the Revit external event of the handler, bound to the add-in the handler belongs to.
    /// </summary>
    /// <param name="handler">The external event handler.</param>
    /// <param name="source">The delegate the handler invokes, or <see langword="null" /> when the handler type implements the execution logic.</param>
    /// <returns>The Revit external event.</returns>
    /// <remarks>
    ///     Inside a Revit callback, Revit binds the event to the active add-in.
    ///     The call captures the active add-in for the handler assembly.
    ///     Outside any callback, the event receives the captured add-in of the assembly of <paramref name="source" />, or of the handler type.
    /// </remarks>
    internal static ExternalEvent CreateExternalEvent(IExternalEventHandler handler, Delegate? source)
    {
        if (!IsSupported)
        {
            return ExternalEvent.Create(handler);
        }

        var owner = FindOwner(handler, source);
        if (IsAddInActive())
        {
            CaptureOwner(owner);
            return ExternalEvent.Create(handler);
        }

        if (!TryGetAddInData(owner, out var addInData))
        {
            return ExternalEvent.Create(handler);
        }

        // The native scope keeps its state in its own memory: the buffer stays in this frame until the destructor runs.
        var scope = stackalloc byte[AddInDataScopeSize];
        EnterAddInDataScope(scope, (void*)addInData);
        try
        {
            return ExternalEvent.Create(handler);
        }
        finally
        {
            ExitAddInDataScope(scope);
        }
    }

    private static void CaptureOwner(Assembly? owner)
    {
        if (TryGetAddInData(owner, out _) || !IsAddInActive())
        {
            return;
        }

        lock (CaptureLock)
        {
            if (TryGetAddInData(owner, out _))
            {
                return;
            }

            var addInData = Marshal.AllocHGlobal(AddInDataSize);
            GetActiveAddInData((void*)addInData);
            if (owner is null)
            {
                Volatile.Write(ref _isolatedAddInData, addInData);
            }
            else
            {
                SharedAddIns.Add(owner, new CapturedAddIn(addInData));
            }
        }
    }

    private static bool TryGetAddInData(Assembly? owner, out IntPtr addInData)
    {
        if (owner is null)
        {
            addInData = Volatile.Read(ref _isolatedAddInData);
            return addInData != IntPtr.Zero;
        }

        if (SharedAddIns.TryGetValue(owner, out var capturedAddIn))
        {
            addInData = capturedAddIn.AddInData;
            return true;
        }

        addInData = IntPtr.Zero;
        return false;
    }

    // Revit registers an external event for an add-in only when the add-in data stack holds one.
    // The active add-in data can't tell an empty stack: it carries the null GUID before Revit 2027 and a fixed non-null GUID since.
    private static bool IsAddInActive()
    {
        return !IsAddInDataStackEmpty(GetAddInDataStack());
    }

    // An isolated toolkit keeps one add-in for every assembly.
    // A shared toolkit keys the add-in by the assembly of the execution logic.
    // The delegate target is the closure, the lambda cache, or the instance of the consumer code.
    // The target comes first: Delegate.Method runs a reflection lookup and allocates on every new delegate instance.
    private static Assembly? FindOwner(IExternalEventHandler handler, Delegate? source)
    {
        if (!IsShared)
        {
            return null;
        }

        if (source is null)
        {
            return handler.GetType().Assembly;
        }

        return source.Target?.GetType().Assembly ?? source.Method.Module.Assembly;
    }

    private static MethodInfo? FindGlobalMethod(Module module, string name, params Type[] parameterTypes)
    {
        return module.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static, binder: null, CallingConventions.Any, parameterTypes, modifiers: null);
    }

    private sealed class CapturedAddIn(IntPtr addInData)
    {
        public IntPtr AddInData { get; } = addInData;
    }
}
