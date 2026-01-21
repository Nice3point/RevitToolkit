using System.Collections.Concurrent;

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     Implementation for an asynchronous Revit <see cref="Autodesk.Revit.UI.IExternalCommand"/>.
/// </summary>
/// <remarks>
///     <para>
///         This command executes async code while pumping continuations on the Revit thread.
///         The Revit UI will remain blocked until the async operation completes.
///     </para>
///     <para>
///         Recommended for I/O-bound operations such as HTTP requests, file access, or database queries.
///         For CPU-bound work, use <see cref="Nice3point.Revit.Toolkit.External.ExternalCommand"/> with background threads.
///     </para>
///     <para>
///         For long-running operations that should not block the UI, consider using 
///         <see cref="Nice3point.Revit.Toolkit.External.Handlers.AsyncEventHandler"/> pattern instead.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public class SampleAsyncCommand : AsyncExternalCommand
///         {
///             public override async Task ExecuteAsync()
///             {
///                 var httpClient = new HttpClient();
///                 var response = await httpClient.GetStringAsync("https://api.example.com/data");
///                 
///                 // Back on Revit thread - safe to use Revit API
///                 TaskDialog.Show("Result", response);
///             }
///         }
///     </code>
/// </example>
[PublicAPI]
public abstract class AsyncExternalCommand : ExternalCommand
{
    /// <summary>
    ///     Overload this method to implement an asynchronous external command within Revit.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The method executes on the Revit main thread with async/await support.
    ///         All continuations after await are marshaled back to the Revit thread.
    ///     </para>
    ///     <para>
    ///         The Revit API can be safely accessed both before and after await expressions.
    ///     </para>
    /// </remarks>
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

        ExecuteWithMessagePump(task);
        task.GetAwaiter().GetResult();
    }

    private static void ExecuteWithMessagePump(Task task)
    {
        var previousContext = SynchronizationContext.Current;
        using var workAvailableEvent = new ManualResetEventSlim(false);

        var synchronizationContext = new RevitSynchronizationContext(workAvailableEvent);
        var taskScheduler = new RevitTaskScheduler(workAvailableEvent);

        SynchronizationContext.SetSynchronizationContext(synchronizationContext);

        try
        {
            task.ContinueWith(
                static _ => { },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                taskScheduler);

            PumpUntilComplete(task, synchronizationContext, taskScheduler, workAvailableEvent);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    private static void PumpUntilComplete(
        Task task,
        RevitSynchronizationContext synchronizationContext,
        RevitTaskScheduler taskScheduler,
        ManualResetEventSlim workAvailableEvent)
    {
        const int maxSpinIterations = 50;
        const int eventWaitTimeoutMs = 50;

        var spinWait = new SpinWait();

        while (!task.IsCompleted)
        {
            var hasWork = synchronizationContext.ProcessPendingWork();
            hasWork |= taskScheduler.ProcessPendingTasks();

            if (hasWork)
            {
                spinWait.Reset();
                continue;
            }

            if (spinWait.Count < maxSpinIterations)
            {
                spinWait.SpinOnce();
            }
            else
            {
                workAvailableEvent.Wait(eventWaitTimeoutMs);
                workAvailableEvent.Reset();
                spinWait.Reset();
            }
        }

        while (synchronizationContext.ProcessPendingWork() || taskScheduler.ProcessPendingTasks())
        {
        }
    }

    /// <summary>
    ///     SynchronizationContext that queues continuations for execution on the Revit thread.
    /// </summary>
    private sealed class RevitSynchronizationContext(ManualResetEventSlim workAvailableEvent) : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _workQueue = new();

        public override void Post(SendOrPostCallback callback, object? state)
        {
            _workQueue.Enqueue((callback, state));
            workAvailableEvent.Set();
        }

        public override void Send(SendOrPostCallback callback, object? state)
        {
            callback(state);
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        public bool ProcessPendingWork()
        {
            var hasWork = false;

            while (_workQueue.TryDequeue(out var item))
            {
                hasWork = true;
                item.Callback(item.State);
            }

            return hasWork;
        }
    }

    /// <summary>
    ///     TaskScheduler that executes tasks on the Revit thread.
    /// </summary>
    private sealed class RevitTaskScheduler(ManualResetEventSlim workAvailableEvent) : TaskScheduler
    {
        private readonly ConcurrentQueue<Task> _taskQueue = new();

        public override int MaximumConcurrencyLevel => 1;

        protected override void QueueTask(Task task)
        {
            _taskQueue.Enqueue(task);
            workAvailableEvent.Set();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _taskQueue;
        }

        public bool ProcessPendingTasks()
        {
            var hasWork = false;

            while (_taskQueue.TryDequeue(out var task))
            {
                hasWork = true;
                TryExecuteTask(task);
            }

            return hasWork;
        }
    }
}