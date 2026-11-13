using System.Collections.Concurrent;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Nice3point.Revit.Toolkit.Internal;

/// <summary>
///     Provides the synchronous wait for an asynchronous method that pumps the messages of the calling thread.
/// </summary>
/// <remarks>
///     Revit shuts down the WPF dispatcher of its thread after the main window unloads, and the dispatcher pumps no <see cref="DispatcherFrame" /> from that moment.
/// </remarks>
internal static class MessagePump
{
    /// <summary>
    ///     Runs an asynchronous method on the calling thread and pumps the messages of the thread until the method completes.
    /// </summary>
    /// <param name="asyncMethod">The asynchronous method to run.</param>
    /// <remarks>
    ///     <para>
    ///         A thread with a running dispatcher pumps a <see cref="DispatcherFrame" />.
    ///         The frame continues when the dispatcher starts its shutdown, and the dispatcher finishes the shutdown after the frame exits.
    ///     </para>
    ///     <para>
    ///         A thread whose dispatcher has shut down runs the continuations of the method on the calling thread and dispatches the window messages of the thread.
    ///     </para>
    ///     <para>
    ///         The method rethrows the exception of the asynchronous method.
    ///     </para>
    /// </remarks>
    public static void Run(Func<Task> asyncMethod)
    {
        if (Dispatcher.CurrentDispatcher.HasShutdownFinished)
        {
            RunWithoutDispatcher(asyncMethod);
            return;
        }

        var task = asyncMethod();
        if (!task.IsCompleted)
        {
            // A frame that exits on the shutdown request of the dispatcher returns before the task completes,
            // and the blocking wait that follows deadlocks against the continuation posted to this thread.
            var frame = new DispatcherFrame(false);

            // TaskScheduler.Default ensures continuation runs on ThreadPool, not UI thread.
            // Prevents deadlock: if continuation ran on UI thread via SynchronizationContext,
            // it would wait for PushFrame to finish, which waits for continuation - deadlock.
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);

            Dispatcher.PushFrame(frame);
        }

        //Rethrow exceptions
        task.GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Runs an asynchronous method on a thread whose dispatcher has shut down.
    /// </summary>
    /// <param name="asyncMethod">The asynchronous method to run.</param>
    /// <remarks>
    ///     A dispatcher that has shut down aborts every operation posted to it, including the continuations of a <see cref="DispatcherSynchronizationContext" />.
    ///     The method installs a <see cref="CallbackQueueSynchronizationContext" /> for the duration of the call and restores the previous context afterward.
    /// </remarks>
    private static void RunWithoutDispatcher(Func<Task> asyncMethod)
    {
        var previousContext = SynchronizationContext.Current;
        using var context = new CallbackQueueSynchronizationContext();

        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var task = asyncMethod();
            if (!task.IsCompleted)
            {
                context.RunCallbacks(task);
            }

            //Rethrow exceptions
            task.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    /// <summary>
    ///     Represents a synchronization context that queues the posted callbacks and runs them on the thread that created it.
    /// </summary>
    private sealed class CallbackQueueSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _callbacks = new();
        private readonly AutoResetEvent _signal = new(false);
        private readonly Lock _signalLock = new();
        private readonly int _threadId = Environment.CurrentManagedThreadId;
        private bool _disposed;

        /// <inheritdoc />
        public override void Post(SendOrPostCallback callback, object? state)
        {
            _callbacks.Enqueue((callback, state));
            Signal();
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Another thread sends the callback.</exception>
        /// <remarks>The context runs a callback sent from its own thread inline.</remarks>
        public override void Send(SendOrPostCallback callback, object? state)
        {
            if (Environment.CurrentManagedThreadId != _threadId)
            {
                throw new NotSupportedException("A synchronous callback must be sent from the thread that created the synchronization context.");
            }

            callback(state);
        }

        /// <inheritdoc />
        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        /// <summary>
        ///     Runs the posted callbacks and dispatches the window messages of the calling thread until a task completes.
        /// </summary>
        /// <param name="task">The task that ends the wait.</param>
        /// <remarks>
        ///     The method removes a <c>WM_QUIT</c> message from the queue for the duration of the wait and posts it again before it returns.
        /// </remarks>
        public void RunCallbacks(Task task)
        {
            // A task completed by another thread posts no callback to the queue.
            task.ContinueWith(_ => Signal(), TaskScheduler.Default);

            int? exitCode = null;
            try
            {
                while (true)
                {
                    while (_callbacks.TryDequeue(out var callback))
                    {
                        callback.Callback(callback.State);
                    }

                    if (task.IsCompleted)
                    {
                        return;
                    }

                    if (WaitForSignalOrMessages())
                    {
                        DispatchMessages(ref exitCode);
                    }
                }
            }
            finally
            {
                if (exitCode is not null)
                {
                    PInvoke.PostQuitMessage(exitCode.Value);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_signalLock)
            {
                _disposed = true;
                _signal.Dispose();
            }
        }

        /// <summary>
        ///     Wakes the thread that waits in <see cref="RunCallbacks" />.
        /// </summary>
        /// <remarks>
        ///     A disposed context ignores the call: a callback posted after the asynchronous method completes never runs.
        /// </remarks>
        private void Signal()
        {
            lock (_signalLock)
            {
                if (!_disposed)
                {
                    _signal.Set();
                }
            }
        }

        /// <summary>
        ///     Blocks the calling thread until the context is signaled or the message queue of the thread has input.
        /// </summary>
        /// <returns><see langword="true" /> if the message queue has input; otherwise, <see langword="false" />.</returns>
        private unsafe bool WaitForSignalOrMessages()
        {
            var signal = (HANDLE)_signal.SafeWaitHandle.DangerousGetHandle();
            var waitEvent = PInvoke.MsgWaitForMultipleObjectsEx(
                1,
                &signal,
                PInvoke.INFINITE,
                QUEUE_STATUS_FLAGS.QS_ALLINPUT,
                MSG_WAIT_FOR_MULTIPLE_OBJECTS_EX_FLAGS.MWMO_INPUTAVAILABLE);

            return waitEvent == WAIT_EVENT.WAIT_OBJECT_0 + 1;
        }

        /// <summary>
        ///     Dispatches every message in the queue of the calling thread.
        /// </summary>
        /// <param name="exitCode">When this method returns, contains the exit code of the <c>WM_QUIT</c> message if the queue had one.</param>
        private static void DispatchMessages(ref int? exitCode)
        {
            while (PInvoke.PeekMessage(out var message, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
            {
                if (message.message == PInvoke.WM_QUIT)
                {
                    exitCode = (int)message.wParam.Value;
                    continue;
                }

                PInvoke.TranslateMessage(message);
                PInvoke.DispatchMessage(message);
            }
        }
    }
}
