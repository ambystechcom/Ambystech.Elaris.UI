using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Ambystech.Elaris.UI;

/// <summary>
/// Provides data for <see cref="Application.UnhandledException"/>.
/// </summary>
public sealed class ApplicationExceptionEventArgs(Exception exception) : EventArgs
{
    /// <summary>
    /// Gets the exception.
    /// </summary>
    public Exception Exception { get; } = exception;

    /// <summary>
    /// Gets or sets whether the exception was handled. When <c>false</c> (the default),
    /// the application stops and <see cref="Application.Run"/> rethrows the exception
    /// after restoring the terminal.
    /// </summary>
    public bool Handled { get; set; }
}

public partial class Application
{
    private readonly ConcurrentQueue<WorkItem> _workQueue = new();
    private volatile int _uiThreadId = -1;

    private readonly record struct WorkItem(Action Run, Action? Cancel);

    /// <summary>
    /// Raised when a key handler, or an action queued with <see cref="Post"/>, throws on the UI thread.
    /// Set <see cref="ApplicationExceptionEventArgs.Handled"/> to keep the application running.
    /// </summary>
    public event EventHandler<ApplicationExceptionEventArgs>? UnhandledException;

    /// <summary>
    /// Returns <c>true</c> when called on the thread running <see cref="Run"/>.
    /// Widgets should only be changed on that thread.
    /// </summary>
    public bool CheckAccess() => Environment.CurrentManagedThreadId == _uiThreadId;

    /// <summary>
    /// Queues an action to run on the UI thread, then redraws. Can be called from any thread,
    /// including before <see cref="Run"/> starts. Actions run in the order they were queued.
    /// </summary>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Enqueue(new WorkItem(action, null));
    }

    /// <summary>
    /// Runs an action on the UI thread and completes when it has run. If called on the UI
    /// thread, the action runs immediately. If the application stops first, the task is cancelled.
    /// </summary>
    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return InvokeAsync(() =>
        {
            action();
            return true;
        });
    }

    /// <summary>
    /// Runs a function on the UI thread and returns its result. If called on the UI thread,
    /// the function runs immediately. If the application stops first, the task is cancelled.
    /// </summary>
    public Task<T> InvokeAsync<T>(Func<T> function)
    {
        ArgumentNullException.ThrowIfNull(function);

        if (CheckAccess())
        {
            try
            {
                var result = function();
                RequestRender();
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return Task.FromException<T>(ex);
            }
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        Enqueue(new WorkItem(
            () =>
            {
                try
                {
                    completion.TrySetResult(function());
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            },
            () => completion.TrySetCanceled()));

        return completion.Task;
    }

    private void Enqueue(WorkItem item)
    {
        _workQueue.Enqueue(item);
        _wake.Set();
    }

    /// <summary>
    /// Runs the actions that were queued when this call started. Actions queued while
    /// these run wait for the next loop iteration, so a busy producer can't starve input.
    /// </summary>
    /// <returns><c>true</c> if any action ran.</returns>
    private bool RunQueuedActions()
    {
        int count = _workQueue.Count;
        bool ranAny = false;

        for (int i = 0; i < count && _running; i++)
        {
            if (!_workQueue.TryDequeue(out var item))
                break;

            ranAny = true;
            RunOnUiThread(item.Run);
        }

        return ranAny;
    }

    /// <summary>
    /// Cancels actions still queued when the application stops.
    /// </summary>
    private void CancelQueuedActions()
    {
        while (_workQueue.TryDequeue(out var item))
            item.Cancel?.Invoke();
    }

    /// <summary>
    /// Runs UI-thread code, routing exceptions to <see cref="UnhandledException"/>.
    /// Unhandled exceptions stop the loop and are rethrown from <see cref="Run"/>.
    /// </summary>
    private void RunOnUiThread(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var args = new ApplicationExceptionEventArgs(ex);
            UnhandledException?.Invoke(this, args);

            if (!args.Handled)
            {
                _fatalException ??= ExceptionDispatchInfo.Capture(ex);
                Stop();
            }
        }
    }

    /// <summary>
    /// The <see cref="SynchronizationContext"/> installed on the UI thread while the application
    /// runs, so <c>await</c> in UI code resumes on the UI thread.
    /// </summary>
    private sealed class ElarisSynchronizationContext(Application application) : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
            => application.Post(() => d(state));

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (application.CheckAccess())
                d(state);
            else
                application.InvokeAsync(() => d(state)).GetAwaiter().GetResult();
        }

        public override SynchronizationContext CreateCopy() => this;
    }
}
