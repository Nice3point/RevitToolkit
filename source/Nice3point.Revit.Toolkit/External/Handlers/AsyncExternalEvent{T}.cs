using System.ComponentModel;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Handler to provide access to modify the Revit document asynchronously with a return value.
/// </summary>
[PublicAPI]
public sealed class AsyncExternalEvent<T> : ExternalEventHandler, IAsyncExternalEvent<T>
{
    private readonly Func<T>? _handler;
    private readonly Func<UIApplication, T>? _uiHandler;
    private TaskCompletionSource<T>? _taskCompletionSource;

    /// <summary>
    ///     Handler to provide access to modify the Revit document asynchronously with a return value.
    /// </summary>
    public AsyncExternalEvent(Func<T> handler)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Handler to provide access to modify the Revit document asynchronously with a return value.
    /// </summary>
    public AsyncExternalEvent(Func<UIApplication, T> handler)
    {
        _uiHandler = handler;
    }

    /// <summary>Callback invoked by Revit. Not used to be called in user code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void Execute(UIApplication uiApplication)
    {
        try
        {
            var result = ExecuteHandler(uiApplication);
            _taskCompletionSource?.SetResult(result);
        }
        catch (Exception exception)
        {
            _taskCompletionSource?.SetException(exception);
        }
    }

    /// <summary>
    ///     Instructing Revit to queue a handler, raise the external event and await completion with a result.
    /// </summary>
    public Task<T> RaiseAsync()
    {
        if (RevitContext.IsRevitInApiMode)
        {
            try
            {
                var result = ExecuteHandler(RevitContext.UiApplication);
                return Task.FromResult(result);
            }
            catch (Exception exception)
            {
                return Task.FromException<T>(exception);
            }
        }

        _taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Raise();

        return _taskCompletionSource.Task;
    }
    
    private T ExecuteHandler(UIApplication uiApplication)
    {
        T result;
        if (_uiHandler is not null)
        {
            result = _uiHandler.Invoke(uiApplication);
        }
        else
        {
            result = _handler!.Invoke();
        }

        return result;
    }
}