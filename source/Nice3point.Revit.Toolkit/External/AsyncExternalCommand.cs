using System.Windows.Threading;

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Implementation for an asynchronous Revit <see cref="Autodesk.Revit.UI.IExternalCommand" />.
/// </summary>
/// <remarks>
///     <para>
///         Enables async/await patterns within Revit external commands while maintaining
///         execution on the Revit main thread. The Revit UI remains responsive during async operations
///         through dispatcher message pumping.
///     </para>
///     <para>
///         Ideal for I/O-bound operations such as HTTP requests, file operations, or database queries.
///         Revit API calls can be made before and after await points since execution resumes on the main thread.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     public override async Task ExecuteAsync()
///     {
///         var selectedIds = ActiveUiDocument.Selection.GetElementIds();
///     
///         await Task.Delay(1000);
///     
///         using var httpClient = new HttpClient();
///         var response = await httpClient.GetStringAsync("https://api.example.com/data");
///     
///         using var transaction = new Transaction(ActiveDocument, "Update Parameters");
///         transaction.Start();
///     
///         foreach (var id in selectedIds)
///         {
///             var element = ActiveDocument.GetElement(id);
///             element?.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(response);
///         }
///     
///         transaction.Commit();
///     }
///     </code>
/// </example>
[PublicAPI]
public abstract class AsyncExternalCommand : ExternalCommand
{
    /// <summary>
    ///     Overload this method to implement an asynchronous external command within Revit.
    /// </summary>
    public abstract Task ExecuteAsync();

    /// <summary>
    ///     Synchronous execution wrapper with message pumping.
    /// </summary>
    public sealed override void Execute()
    {
        var task = ExecuteAsync();
        if (task.IsCompleted)
        {
            //Rethrow exceptions
            task.GetAwaiter().GetResult();
            return;
        }

        var frame = new DispatcherFrame();

        // TaskScheduler.Default ensures continuation runs on ThreadPool, not UI thread.
        // Prevents deadlock: if continuation ran on UI thread via SynchronizationContext,
        // it would wait for PushFrame to finish, which waits for continuation - deadlock.
        task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);

        Dispatcher.PushFrame(frame);

        task.GetAwaiter().GetResult();
    }
}