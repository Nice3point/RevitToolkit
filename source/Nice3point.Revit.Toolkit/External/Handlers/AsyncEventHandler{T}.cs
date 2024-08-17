﻿using System.ComponentModel;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.External.Handlers;

/// <summary>
///     Handler, to provide access to change the Revit document
/// </summary>
/// <remarks>Suitable for cases where it is needed to await the completion of an external event with the return of a value</remarks>
[PublicAPI]
public sealed class AsyncEventHandler<T> : ExternalEventHandler
{
    private Func<UIApplication, T>? _func;
    private TaskCompletionSource<T>? _resultTask;

    /// <summary>
    ///     This method is called to handle the external event
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        if (_resultTask is null)
        {
            throw new InvalidOperationException("RaiseAsync() must be called instead of using Execute callback");
        }
        
        if (_func is null)
        {
            throw new InvalidOperationException("RaiseAsync() must be called with non-null handler");
        }
        
        try
        {
            var result = _func(uiApplication);
            _resultTask.SetResult(result);
        }
        catch (Exception exception)
        {
            _resultTask.SetException(exception);
        }
        finally
        {
            _func = null;
            _resultTask = null;
        }
    }

    /// <summary>
    ///     Instructing Revit to queue a handler, raise (signal) the external event and async awaiting for its completion
    /// </summary>
    /// <returns>
    ///     The return value of the method that delegate encapsulates
    /// </returns>
    /// <remarks>
    ///     This method async awaiting completion of the <see cref="Nice3point.Revit.Toolkit.External.Handlers.AsyncEventHandler.Execute" /> method. <br />
    ///     Exceptions in the delegate will not be ignored and will be rethrown in the original synchronization context.<br />
    ///     Use <see langword="await" /> to awaiting.
    ///     <see cref="System.Threading.Tasks.Task.WaitAll(System.Threading.Tasks.Task[])" />, <see cref="System.Threading.Tasks.Task.Wait()" /> will cause a deadlock.
    /// </remarks>
    public async Task<T> RaiseAsync(Func<UIApplication, T> func)
    {
        if (_func is null) _func = func;
        else _func += func;

        Raise();

        _resultTask ??= new TaskCompletionSource<T>();
        return await _resultTask.Task;
    }
}