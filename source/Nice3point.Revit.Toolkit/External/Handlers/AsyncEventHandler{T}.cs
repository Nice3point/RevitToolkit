using System.ComponentModel;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.External.Handlers;

/// <summary>
///     Handler, to provide access to modify the Revit document.
/// </summary>
/// <remarks>Suitable for cases where it is needed to await the completion of an external event with the return of a value.</remarks>
[PublicAPI]
public sealed class AsyncEventHandler<T> : ExternalEventHandler
{
    private Func<UIApplication, T>? _handler;
    private TaskCompletionSource<T>? _resultTask;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    /// <summary>
    ///     This method is called to handle the external event.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        if (_handler is null) return;
        if (_resultTask is null) return;
        
        try
        {
            var result = _handler(uiApplication);
            _resultTask.SetResult(result);
        }
        catch (Exception exception)
        {
            _resultTask.SetException(exception);
        }
        finally
        {
            _handler = null;
            _resultTask = null;
        }
    }

    /// <summary>
    ///     Instructing Revit to queue a handler, raise (signal) the external event and async awaiting for its completion.
    /// </summary>
    /// <returns>
    ///     The return value of the method that delegate encapsulates.
    /// </returns>
    /// <remarks>
    ///     This method async awaiting completion of the <see cref="Nice3point.Revit.Toolkit.External.Handlers.AsyncEventHandler.Execute" /> method. <br />
    ///     Exceptions in the delegate will not be ignored and will be rethrown in the original synchronization context.<br />
    ///     <see cref="System.Threading.Tasks.Task.WaitAll(System.Threading.Tasks.Task[])" />,
    ///     <see cref="System.Threading.Tasks.Task.Wait()" /> will cause a deadlock.<br/><br/>
    ///     Executes the handler out of queue if Revit is in API mode.
    /// </remarks>
    public async Task<T> RaiseAsync(Func<UIApplication, T> handler)
    {
        if (Context.IsRevitInApiMode)
        {
            return handler(Context.UiApplication)!;
        }
        
        await _semaphore.WaitAsync();

        try
        {
            _handler = handler;
            _resultTask = new TaskCompletionSource<T>();
        
            Raise();
            return await _resultTask.Task;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}