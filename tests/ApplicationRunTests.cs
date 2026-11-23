using Ambystech.Elaris.UI.Terminal;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests;

/// <summary>
/// Runs the real event loop against a <see cref="VirtualTerminal"/>.
/// </summary>
public class ApplicationRunTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static Task RunInBackground(Application app, Ambystech.Elaris.UI.Widgets.Widget root)
        => Task.Factory.StartNew(() => app.Run(root), TaskCreationOptions.LongRunning);

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

    [Fact]
    public void Run_DrawsWidgets_AndRestoresTerminalOnQuit()
    {
        var terminal = new VirtualTerminal(40, 6);
        var app = new Application(terminal) { TargetFps = 200 };
        var frame = new Frame("Title");
        frame.Add(new Label("Hello from Elaris") { X = 2, Y = 2, Width = 30, Height = 1 });

        var run = RunInBackground(app, frame);
        WaitUntil(() => terminal.GetText().Contains("Hello from Elaris"), "the label is drawn");

        terminal.IsAlternateScreen.Should().BeTrue();
        terminal.IsCursorVisible.Should().BeFalse();
        terminal.TreatControlCAsInput.Should().BeTrue();
        terminal.GetText().Should().Contain("Title");

        terminal.EnqueueKey(ConsoleKey.Escape);
        run.Wait(Timeout).Should().BeTrue("Escape should stop the application");

        terminal.IsAlternateScreen.Should().BeFalse();
        terminal.IsCursorVisible.Should().BeTrue();
        terminal.IsInitialized.Should().BeFalse();
        terminal.TreatControlCAsInput.Should().BeFalse();
    }

    [Fact]
    public void Run_TypedTextReachesFocusedTextField_InOneBurst()
    {
        var terminal = new VirtualTerminal(40, 3);
        var app = new Application(terminal) { TargetFps = 200 };
        var root = new Container();
        var field = new TextField { X = 0, Y = 0, Width = 30, Height = 1 };
        root.Add(field);

        // Queue everything before the loop starts, like a paste arriving at once.
        terminal.EnqueueText("pasted text");
        terminal.EnqueueKey(ConsoleKey.C, 'c', control: true);

        var run = RunInBackground(app, root);
        run.Wait(Timeout).Should().BeTrue("Ctrl+C should stop the application");

        field.Text.Should().Be("pasted text");
    }

    [Fact]
    public void Run_ReactsToTerminalResize()
    {
        var terminal = new VirtualTerminal(20, 4);
        var app = new Application(terminal) { TargetFps = 200 };
        var root = new Container();

        var run = RunInBackground(app, root);
        WaitUntil(() => app.Screen != null, "the screen is created");

        terminal.Resize(30, 6);
        WaitUntil(() => app.Screen!.Width == 30 && app.Screen.Height == 6, "the screen is resized");
        root.Bounds.Width.Should().Be(30);
        root.Bounds.Height.Should().Be(6);

        app.Stop();
        run.Wait(Timeout).Should().BeTrue();
    }
}
