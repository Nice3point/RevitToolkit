using System.Windows.Threading;
using Nice3point.Revit.Toolkit.External;
using Nice3point.TUnit.Revit;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class AsyncExternalApplicationTests : RevitApiUiTest
{
    private const int QuitExitCode = 7;
    private const uint TimerIntervalMilliseconds = 10;

    [Test]
    [Timeout(10_000)]
    public async Task OnShutdown_DispatcherRunning_ContinuesOnTheCallingThread(CancellationToken cancellationToken)
    {
        // Act
        var (callingThreadId, continuationThreadId) = await RunOnDispatcherThreadAsync(static () =>
        {
            var continuationThreadId = 0;
            var application = new TestApplication(async () =>
            {
                await Task.Yield();
                continuationThreadId = Environment.CurrentManagedThreadId;
            });

            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            application.OnShutdown();

            return (Environment.CurrentManagedThreadId, continuationThreadId);
        }).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(continuationThreadId).IsEqualTo(callingThreadId);
    }

    [Test]
    [Timeout(10_000)]
    public async Task OnShutdown_DispatcherShutdownPending_ContinuesOnTheCallingThread(CancellationToken cancellationToken)
    {
        // Act
        var (callingThreadId, continuationThreadId) = await RunOnDispatcherThreadAsync(static () =>
        {
            var continuationThreadId = 0;
            var dispatcher = Dispatcher.CurrentDispatcher;
            var application = new TestApplication(async () =>
            {
                await Task.Yield();
                continuationThreadId = Environment.CurrentManagedThreadId;
            });

            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            application.OnShutdown();

            return (Environment.CurrentManagedThreadId, continuationThreadId);
        }).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(continuationThreadId).IsEqualTo(callingThreadId).Because("the frame continues after the dispatcher starts its shutdown");
    }

    [Test]
    [Timeout(10_000)]
    public async Task OnShutdown_DispatcherShutdownPending_FinishesTheDispatcherShutdown(CancellationToken cancellationToken)
    {
        // Act
        var hasShutdownFinished = await RunOnDispatcherThreadAsync(static () =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            var application = new TestApplication(static async () => await Task.Yield());

            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            application.OnShutdown();

            return dispatcher.HasShutdownFinished;
        }).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(hasShutdownFinished).IsTrue();
    }

    [Test]
    [Timeout(10_000)]
    public async Task OnShutdown_DispatcherShutDown_ContinuesOnTheCallingThread(CancellationToken cancellationToken)
    {
        // Act
        var (callingThreadId, continuationThreadId) = await RunOnDispatcherThreadAsync(static () =>
        {
            var continuationThreadId = 0;
            var application = new TestApplication(async () =>
            {
                await Task.Yield();
                continuationThreadId = Environment.CurrentManagedThreadId;
            });

            Dispatcher.CurrentDispatcher.InvokeShutdown();
            application.OnShutdown();

            return (Environment.CurrentManagedThreadId, continuationThreadId);
        }).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(continuationThreadId).IsEqualTo(callingThreadId);
    }

    [Test]
    [Timeout(10_000)]
    public async Task OnShutdown_DispatcherShutDownAndTaskCompletedByAnotherThread_ReturnsAfterTheTask(CancellationToken cancellationToken)
    {
        // Act
        var isShutdownCompleted = await RunOnDispatcherThreadAsync(() =>
        {
            var isShutdownCompleted = false;
            var application = new TestApplication(async () =>
            {
                await Task.Run(static () => { }, cancellationToken).ConfigureAwait(false);
                isShutdownCompleted = true;
            });

            Dispatcher.CurrentDispatcher.InvokeShutdown();
            application.OnShutdown();

            return isShutdownCompleted;
        }).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(isShutdownCompleted).IsTrue();
    }

    [Test]
    [Timeout(10_000)]
    public async Task OnShutdown_DispatcherShutDown_RestoresTheSynchronizationContext(CancellationToken cancellationToken)
    {
        // Act
        var (previousContext, restoredContext) = await RunOnDispatcherThreadAsync(static () =>
        {
            var previousContext = new SynchronizationContext();
            var application = new TestApplication(static async () => await Task.Yield());

            SynchronizationContext.SetSynchronizationContext(previousContext);
            Dispatcher.CurrentDispatcher.InvokeShutdown();
            application.OnShutdown();

            return (previousContext, SynchronizationContext.Current);
        }).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(restoredContext).IsSameReferenceAs(previousContext);
    }

    [Test]
    [Timeout(10_000)]
    public async Task OnShutdown_DispatcherShutDown_RethrowsTheShutdownException(CancellationToken cancellationToken)
    {
        // Act
        var shutdownTask = RunOnDispatcherThreadAsync(static () =>
        {
            var application = new TestApplication(static async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("The shutdown failed.");
            });

            Dispatcher.CurrentDispatcher.InvokeShutdown();
            application.OnShutdown();

            return true;
        });

        // Assert
        await Assert.That(() => shutdownTask.WaitAsync(cancellationToken))
            .Throws<InvalidOperationException>()
            .WithMessage("The shutdown failed.");
    }

    [Test]
    [Timeout(10_000)]
    public async Task OnShutdown_DispatcherShutDown_DispatchesTheWindowMessages(CancellationToken cancellationToken)
    {
        // Act
        var isTimerMessageDispatched = await RunOnDispatcherThreadAsync(static () =>
        {
            Dispatcher.CurrentDispatcher.InvokeShutdown();

            return ShutdownAfterTimerMessage();
        }).WaitAsync(cancellationToken);

        // Assert
        await Assert.That(isTimerMessageDispatched).IsTrue();
    }

    [Test]
    [Timeout(10_000)]
    public async Task OnShutdown_DispatcherShutDownAndQuitMessageInTheQueue_PostsTheQuitMessageAgain(CancellationToken cancellationToken)
    {
        // Act
        var (hasQuitMessage, exitCode) = await RunOnDispatcherThreadAsync(static () =>
        {
            Dispatcher.CurrentDispatcher.InvokeShutdown();
            TestPInvoke.PostQuitMessage(QuitExitCode);

            ShutdownAfterTimerMessage();

            var hasQuitMessage = TestPInvoke.PeekMessage(out var message, HWND.Null, TestPInvoke.WM_QUIT, TestPInvoke.WM_QUIT, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE);
            return ((bool)hasQuitMessage, (int)message.wParam.Value);
        }).WaitAsync(cancellationToken);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(hasQuitMessage).IsTrue();
            await Assert.That(exitCode).IsEqualTo(QuitExitCode);
        }
    }

    /// <summary>
    ///     Shuts down an application that awaits the timer message of the calling thread.
    /// </summary>
    /// <returns><see langword="true" /> if the shutdown completed after the timer message; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     <c>DispatchMessage</c> calls the timer procedure: the shutdown never completes on a thread that dispatches no window messages.
    /// </remarks>
    private static bool ShutdownAfterTimerMessage()
    {
        var isShutdownCompleted = false;
        var timerMessage = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var application = new TestApplication(async () =>
        {
            await timerMessage.Task;
            isShutdownCompleted = true;
        });

        TIMERPROC timerProcedure = (_, _, _, _) => timerMessage.TrySetResult(true);
        var timerId = TestPInvoke.SetTimer(HWND.Null, 0, TimerIntervalMilliseconds, timerProcedure);
        try
        {
            application.OnShutdown();
        }
        finally
        {
            TestPInvoke.KillTimer(HWND.Null, timerId);
            GC.KeepAlive(timerProcedure);
        }

        return isShutdownCompleted;
    }

    /// <summary>
    ///     Runs a function on a new STA thread, which has a WPF dispatcher of its own.
    /// </summary>
    /// <typeparam name="TResult">The type of the function result.</typeparam>
    /// <param name="function">The function to run.</param>
    /// <returns>
    ///     A task that represents the asynchronous run operation.
    ///     The task result contains the result of the function.
    /// </returns>
    private static Task<TResult> RunOnDispatcherThreadAsync<TResult>(Func<TResult> function)
    {
        var completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(function());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        })
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completion.Task;
    }

    private sealed class TestApplication(Func<Task> shutdown) : AsyncExternalApplication
    {
        public override Task OnStartupAsync()
        {
            return Task.CompletedTask;
        }

        public override Task OnShutdownAsync()
        {
            return shutdown();
        }
    }
}
