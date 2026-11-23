using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Terminal;
using Ambystech.Elaris.UI.Widgets;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests;

/// <summary>
/// Tests for Post/InvokeAsync, the UI SynchronizationContext, and redraw scheduling.
/// Each test runs the real event loop against a <see cref="VirtualTerminal"/>.
/// </summary>
public class DispatcherTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private sealed class Harness : IDisposable
    {
        public VirtualTerminal Terminal { get; }
        public Application App { get; }
        public Container Root { get; }
        public Task Run { get; }
        public int UiThreadId { get; private set; }

        public Harness(Action<Application, Container>? configure = null, int fps = 100)
        {
            Terminal = new VirtualTerminal(40, 8);
            App = new Application(Terminal) { TargetFps = fps };
            Root = new Container();
            configure?.Invoke(App, Root);

            var started = new ManualResetEventSlim();
            App.Post(() =>
            {
                UiThreadId = Environment.CurrentManagedThreadId;
                started.Set();
            });

            Run = Task.Factory.StartNew(() => App.Run(Root), TaskCreationOptions.LongRunning);
            started.Wait(Timeout).Should().BeTrue("the application should start");
        }

        public void Dispose()
        {
            App.Stop();
            Run.Wait(Timeout);
        }
    }

    private static void WaitUntil(Func<bool> condition, string because)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Timed out waiting until {because}.");
            Thread.Sleep(5);
        }
    }

    private sealed class AsyncKeyWidget : Widget
    {
        public TaskCompletionSource<int> ResumedOnThread { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override bool IsFocusable => true;
        protected override void OnRender(Screen screen) { }

        protected internal override bool OnKeyPress(ConsoleKeyInfo key)
        {
            HandleAsync();
            return true;
        }

        private async void HandleAsync()
        {
            await Task.Delay(20);
            ResumedOnThread.TrySetResult(Environment.CurrentManagedThreadId);
        }
    }

    [Fact]
    public void Post_FromBackgroundThread_RunsOnUiThread()
    {
        using var h = new Harness();
        var ranOn = new TaskCompletionSource<(int thread, bool access)>();

        Task.Run(() => h.App.Post(() =>
            ranOn.SetResult((Environment.CurrentManagedThreadId, h.App.CheckAccess()))));

        ranOn.Task.Wait(Timeout).Should().BeTrue();
        ranOn.Task.Result.thread.Should().Be(h.UiThreadId);
        ranOn.Task.Result.access.Should().BeTrue();
        h.App.CheckAccess().Should().BeFalse("the test thread is not the UI thread");
    }

    [Fact]
    public void Post_RunsActionsInOrder()
    {
        using var h = new Harness();
        var order = new List<int>();
        var done = new ManualResetEventSlim();

        for (int i = 0; i < 100; i++)
        {
            int n = i;
            h.App.Post(() => order.Add(n));
        }
        h.App.Post(done.Set);

        done.Wait(Timeout).Should().BeTrue();
        order.Should().Equal(Enumerable.Range(0, 100));
    }

    [Fact]
    public async Task InvokeAsync_ReturnsResultFromUiThread()
    {
        using var h = new Harness();

        var threadId = await h.App.InvokeAsync(() => Environment.CurrentManagedThreadId).WaitAsync(Timeout);

        threadId.Should().Be(h.UiThreadId);
    }

    [Fact]
    public async Task InvokeAsync_PropagatesExceptionsToCaller_AndKeepsRunning()
    {
        using var h = new Harness();

        var act = () => h.App.InvokeAsync(() => throw new InvalidOperationException("boom")).WaitAsync(Timeout);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        h.Run.IsCompleted.Should().BeFalse("an exception returned to the caller doesn't stop the app");
    }

    [Fact]
    public async Task InvokeAsync_IsCancelled_WhenApplicationStopsFirst()
    {
        var terminal = new VirtualTerminal();
        var app = new Application(terminal);

        // Queued before Run; Run stops on the first key, before running queued actions.
        terminal.EnqueueKey(ConsoleKey.Escape);
        var pending = app.InvokeAsync(() => 42);

        app.Run(new Container());

        var act = () => pending.WaitAsync(Timeout);
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public void Await_InKeyHandler_ResumesOnUiThread()
    {
        var widget = new AsyncKeyWidget();
        using var h = new Harness((_, root) => root.Add(widget));

        h.Terminal.EnqueueKey(ConsoleKey.A, 'a');

        widget.ResumedOnThread.Task.Wait(Timeout).Should().BeTrue();
        widget.ResumedOnThread.Task.Result.Should().Be(h.UiThreadId);
    }

    [Fact]
    public void SynchronizationContext_IsRestored_AfterRun()
    {
        var terminal = new VirtualTerminal();
        var app = new Application(terminal);
        terminal.EnqueueKey(ConsoleKey.Escape);

        var before = SynchronizationContext.Current;
        app.Run(new Container());

        SynchronizationContext.Current.Should().BeSameAs(before);
    }

    [Fact]
    public void UnhandledException_WithoutHandler_StopsAndRethrowsFromRun_AfterRestoringTerminal()
    {
        var terminal = new VirtualTerminal();
        var app = new Application(terminal);
        app.Post(() => throw new InvalidOperationException("fatal"));

        var act = () => app.Run(new Container());

        act.Should().Throw<InvalidOperationException>().WithMessage("fatal");
        terminal.IsAlternateScreen.Should().BeFalse();
        terminal.IsCursorVisible.Should().BeTrue();
    }

    [Fact]
    public void UnhandledException_MarkedHandled_KeepsRunning()
    {
        var seen = new List<Exception>();
        using var h = new Harness((app, _) => app.UnhandledException += (_, e) =>
        {
            seen.Add(e.Exception);
            e.Handled = true;
        });
        var after = new ManualResetEventSlim();

        h.App.Post(() => throw new InvalidOperationException("handled"));
        h.App.Post(after.Set);

        after.Wait(Timeout).Should().BeTrue("actions after a handled exception still run");
        seen.Should().ContainSingle().Which.Message.Should().Be("handled");
        h.Run.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void UnhandledException_IncludesKeyHandlerExceptions()
    {
        var seen = new TaskCompletionSource<Exception>();
        var widget = new ThrowingWidget();
        using var h = new Harness((app, root) =>
        {
            root.Add(widget);
            app.UnhandledException += (_, e) =>
            {
                e.Handled = true;
                seen.TrySetResult(e.Exception);
            };
        });

        h.Terminal.EnqueueKey(ConsoleKey.A, 'a');

        seen.Task.Wait(Timeout).Should().BeTrue();
        seen.Task.Result.Message.Should().Be("from key handler");
    }

    private sealed class ThrowingWidget : Widget
    {
        public override bool IsFocusable => true;
        protected override void OnRender(Screen screen) { }
        protected internal override bool OnKeyPress(ConsoleKeyInfo key) => throw new InvalidOperationException("from key handler");
    }

    [Fact]
    public void Idle_DrawsNoFrames()
    {
        using var h = new Harness();
        WaitUntil(() => h.App.RenderCount > 0, "the first frame is drawn");
        int frames = h.App.RenderCount;
        int flushes = h.Terminal.FlushCount;

        Thread.Sleep(300);

        h.App.RenderCount.Should().Be(frames);
        h.Terminal.FlushCount.Should().Be(flushes);
    }

    [Fact]
    public void AlwaysRender_DrawsEveryFrame()
    {
        using var h = new Harness((app, _) => app.AlwaysRender = true);
        WaitUntil(() => h.App.RenderCount > 0, "the first frame is drawn");
        int frames = h.App.RenderCount;

        Thread.Sleep(300);

        h.App.RenderCount.Should().BeGreaterThan(frames + 5);
    }

    [Fact]
    public void PostedChange_IsDrawn()
    {
        var label = new Label("before") { X = 0, Y = 0, Width = 20, Height = 1 };
        using var h = new Harness((_, root) => root.Add(label));
        WaitUntil(() => h.Terminal.GetLine(0) == "before", "the label is drawn");

        h.App.Post(() => label.Text = "after");

        WaitUntil(() => h.Terminal.GetLine(0) == "after", "the posted change is drawn");
    }

    [Fact]
    public void Invalidate_FromAnotherThread_TriggersRedraw()
    {
        using var h = new Harness();
        WaitUntil(() => h.App.RenderCount > 0, "the first frame is drawn");
        int frames = h.App.RenderCount;

        Task.Run(() => h.Root.Invalidate());

        WaitUntil(() => h.App.RenderCount > frames, "Invalidate causes a redraw");
    }

    [Fact]
    public void BaseWidgetChanges_TriggerRedraw()
    {
        var label = new Label("visible") { X = 0, Y = 0, Width = 20, Height = 1 };
        using var h = new Harness((_, root) => root.Add(label));
        WaitUntil(() => h.Terminal.GetLine(0) == "visible", "the label is drawn");

        // Changed off the UI thread on purpose: base setters call Invalidate().
        h.App.InvokeAsync(() => { }).Wait(Timeout);
        label.Visible = false;

        WaitUntil(() => h.Terminal.GetLine(0) == "", "hiding the label is drawn");
    }

    [Fact]
    public void Redraws_AreLimitedByTargetFps()
    {
        using var h = new Harness(fps: 20);
        WaitUntil(() => h.App.RenderCount > 0, "the first frame is drawn");
        int frames = h.App.RenderCount;

        var stopAt = DateTime.UtcNow.AddMilliseconds(500);
        while (DateTime.UtcNow < stopAt)
        {
            h.Root.Invalidate();
            Thread.Sleep(1);
        }

        // 500 ms at 20 fps is about 10 frames; allow some slack for scheduling.
        (h.App.RenderCount - frames).Should().BeInRange(3, 13);
    }

    [Fact]
    public void AutoComplete_AppliesSuggestionsOnUiThread()
    {
        var popupOpenedOn = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var field = new AutoCompleteTextField
        {
            X = 0, Y = 0, Width = 20, Height = 1,
            DebounceDelay = 10,
            SuggestionsProvider = async text =>
            {
                // Finish on a thread-pool thread, like an HTTP call would.
                await Task.Delay(10).ConfigureAwait(false);
                return ["apple", "apricot"];
            },
        };
        field.PopupOpened += () => popupOpenedOn.TrySetResult(Environment.CurrentManagedThreadId);

        using var h = new Harness((_, root) => root.Add(field));
        h.Terminal.EnqueueText("ap");

        popupOpenedOn.Task.Wait(Timeout).Should().BeTrue("suggestions should appear");
        popupOpenedOn.Task.Result.Should().Be(h.UiThreadId);
    }

    [Fact]
    public void AutoComplete_SlowOlderQuery_DoesNotOverwriteNewerOne()
    {
        var shown = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var field = new AutoCompleteTextField
        {
            X = 0, Y = 0, Width = 20, Height = 1,
            DebounceDelay = 1,
            SuggestionsProvider = async text =>
            {
                // The first query ("a") is slow; the second ("ab") is fast.
                await Task.Delay(text == "a" ? 300 : 10);
                return [$"result for {text}"];
            },
        };
        field.PopupOpened += () => shown.TrySetResult(field.Text);

        using var h = new Harness((_, root) => root.Add(field));
        h.Terminal.EnqueueText("a");
        Thread.Sleep(50);  // let the "a" query start
        h.Terminal.EnqueueText("b");

        shown.Task.Wait(Timeout).Should().BeTrue();
        Thread.Sleep(400); // give the slow "a" query time to finish

        var text = h.App.InvokeAsync(() => h.Terminal.GetText()).Result;
        text.Should().Contain("result for ab");
        System.Text.RegularExpressions.Regex.IsMatch(text, "result for a(?!b)")
            .Should().BeFalse("the slow, older query must not replace the newer suggestions");
    }
}
