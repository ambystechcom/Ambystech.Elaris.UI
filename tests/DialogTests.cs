using System.Drawing;
using Ambystech.Elaris.UI.Input;
using Ambystech.Elaris.UI.Terminal;
using Ambystech.Elaris.UI.Widgets;
using Ambystech.Elaris.UI.Widgets.Dialogs;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests;

public class DialogTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static ConsoleKeyInfo Key(ConsoleKey key, bool shift = false, bool alt = false, bool control = false, char ch = '\0')
        => new(ch, key, shift, alt, control);

    /// <summary>A root with two buttons, attached without running the loop.</summary>
    private static (Application app, Container root, Button first, Button second) CreateApp()
    {
        var app = new Application();
        var root = new Container();
        var first = new Button("First") { Bounds = new Rectangle(0, 0, 10, 3) };
        var second = new Button("Second") { Bounds = new Rectangle(12, 0, 10, 3) };
        root.Add(first);
        root.Add(second);
        app.AttachRoot(root, new Rectangle(0, 0, 80, 24));
        return (app, root, first, second);
    }

    private static (Dialog dialog, Button ok, Button cancel) CreateDialog()
    {
        var dialog = new Dialog("Confirm") { Width = 30, Height = 8 };
        var ok = new Button("OK") { Width = 8, Height = 3 };
        var cancel = new Button("Cancel") { Width = 10, Height = 3 };
        ok.Click += () => dialog.Close(DialogResult.OK);
        cancel.Click += () => dialog.Close(DialogResult.Cancel);
        dialog.Add(ok);
        dialog.Add(cancel);
        return (dialog, ok, cancel);
    }

    [Fact]
    public void ShowDialog_CentersDialog_AndFocusesFirstWidgetInside()
    {
        var (app, _, first, _) = CreateApp();
        var (dialog, ok, _) = CreateDialog();
        app.FocusedWidget.Should().BeSameAs(first);

        app.ShowDialog(dialog);

        dialog.IsOpen.Should().BeTrue();
        app.OpenDialogs.Should().Equal(dialog);
        dialog.Bounds.Should().Be(new Rectangle(25, 8, 30, 8));
        app.FocusedWidget.Should().BeSameAs(ok);
    }

    [Fact]
    public void ShowDialog_UsesInitialFocus()
    {
        var (app, _, _, _) = CreateApp();
        var (dialog, _, cancel) = CreateDialog();
        dialog.InitialFocus = cancel;

        app.ShowDialog(dialog);

        app.FocusedWidget.Should().BeSameAs(cancel);
    }

    [Fact]
    public void Focus_StaysInsideDialog()
    {
        var (app, _, _, _) = CreateApp();
        var (dialog, ok, cancel) = CreateDialog();
        app.ShowDialog(dialog);

        app.GetFocusableWidgets().Should().Equal(ok, cancel);

        app.HandleKey(Key(ConsoleKey.Tab));
        app.FocusedWidget.Should().BeSameAs(cancel);
        app.HandleKey(Key(ConsoleKey.Tab));
        app.FocusedWidget.Should().BeSameAs(ok, "focus wraps around inside the dialog");
        app.HandleKey(Key(ConsoleKey.Tab, shift: true));
        app.FocusedWidget.Should().BeSameAs(cancel);
        app.HandleKey(Key(ConsoleKey.RightArrow, alt: true));
        app.FocusedWidget.Should().BeSameAs(ok);
    }

    [Fact]
    public async Task Close_CompletesTask_AndRestoresFocus()
    {
        var (app, _, _, second) = CreateApp();
        app.SetFocus(second);
        var (dialog, ok, _) = CreateDialog();
        var task = app.ShowDialog(dialog);
        DialogResult? closedWith = null;
        dialog.Closed += r => closedWith = r;

        app.HandleKey(Key(ConsoleKey.Enter, ch: '\r')); // presses OK

        (await task.WaitAsync(Timeout)).Should().Be(DialogResult.OK);
        closedWith.Should().Be(DialogResult.OK);
        dialog.Result.Should().Be(DialogResult.OK);
        dialog.IsOpen.Should().BeFalse();
        app.OpenDialogs.Should().BeEmpty();
        app.FocusedWidget.Should().BeSameAs(second);
        app.GetFocusableWidgets().Should().HaveCount(2, "focus is back in the main screen");
    }

    [Fact]
    public async Task Escape_ClosesDialogWithCancel_AndDoesNotQuit()
    {
        var (app, _, _, _) = CreateApp();
        var (dialog, _, _) = CreateDialog();
        var task = app.ShowDialog(dialog);

        app.HandleKey(Key(ConsoleKey.Escape));

        (await task.WaitAsync(Timeout)).Should().Be(DialogResult.Cancel);
        app.StopRequested.Should().BeFalse();

        app.HandleKey(Key(ConsoleKey.Escape));
        app.StopRequested.Should().BeTrue("with no dialog open, Escape quits again");
    }

    [Fact]
    public void Escape_DoesNothing_WhenCloseOnEscapeIsOff()
    {
        var (app, _, _, _) = CreateApp();
        var (dialog, _, _) = CreateDialog();
        dialog.CloseOnEscape = false;
        app.ShowDialog(dialog);

        app.HandleKey(Key(ConsoleKey.Escape));

        dialog.IsOpen.Should().BeTrue();
        app.StopRequested.Should().BeFalse();
    }

    [Fact]
    public void CtrlC_StillQuits_WithDialogOpen()
    {
        var (app, _, _, _) = CreateApp();
        var (dialog, _, _) = CreateDialog();
        app.ShowDialog(dialog);

        app.HandleKey(Key(ConsoleKey.C, control: true));

        app.StopRequested.Should().BeTrue();
    }

    [Fact]
    public void Mouse_OutsideDialog_IsIgnored_InsideWorks()
    {
        var (app, _, first, _) = CreateApp();
        int firstClicks = 0;
        first.Click += () => firstClicks++;
        var (dialog, ok, _) = CreateDialog();
        var task = app.ShowDialog(dialog);
        ok.Bounds = new Rectangle(dialog.X + 2, dialog.Y + 2, 8, 3);

        app.HandleMouse(new MouseEvent(2, 1, MouseButton.Left, MouseEventKind.Press));
        app.HandleMouse(new MouseEvent(2, 1, MouseButton.Left, MouseEventKind.Release));
        firstClicks.Should().Be(0, "the button behind the dialog can't be clicked");
        app.FocusedWidget.Should().BeSameAs(ok);

        app.HandleMouse(new MouseEvent(ok.X + 1, ok.Y + 1, MouseButton.Left, MouseEventKind.Press));
        app.HandleMouse(new MouseEvent(ok.X + 1, ok.Y + 1, MouseButton.Left, MouseEventKind.Release));
        task.IsCompletedSuccessfully.Should().BeTrue();
        task.Result.Should().Be(DialogResult.OK);
    }

    [Fact]
    public async Task NestedDialogs_FocusTopOne_AndRestoreInOrder()
    {
        var (app, _, first, _) = CreateApp();
        var (outer, outerOk, _) = CreateDialog();
        var (inner, innerOk, _) = CreateDialog();

        var outerTask = app.ShowDialog(outer);
        var innerTask = app.ShowDialog(inner);

        app.FocusedWidget.Should().BeSameAs(innerOk);
        app.GetFocusableWidgets().Should().NotContain(outerOk);

        inner.Close(DialogResult.No);
        (await innerTask.WaitAsync(Timeout)).Should().Be(DialogResult.No);
        app.FocusedWidget.Should().BeSameAs(outerOk);

        outer.Close(DialogResult.Yes);
        (await outerTask.WaitAsync(Timeout)).Should().Be(DialogResult.Yes);
        app.FocusedWidget.Should().BeSameAs(first);
    }

    [Fact]
    public void ShowDialog_Twice_Throws()
    {
        var (app, _, _, _) = CreateApp();
        var (dialog, _, _) = CreateDialog();
        app.ShowDialog(dialog);

        Action act = () => app.ShowDialog(dialog);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShowDialog_InWidgetTree_Throws()
    {
        var (app, root, _, _) = CreateApp();
        var (dialog, _, _) = CreateDialog();
        root.Add(dialog);

        Action act = () => app.ShowDialog(dialog);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Dialog_UsesApplicationTheme()
    {
        var (app, root, _, _) = CreateApp();
        root.Theme = Theme.Light;
        var (dialog, ok, _) = CreateDialog();

        app.ShowDialog(dialog);

        dialog.BackgroundColor.ToArgb().Should().Be(Theme.Light.SurfaceAlt.ToArgb());
        ok.BackgroundColor.ToArgb().Should().Be(Theme.Light.Surface.ToArgb());
    }

    [Fact]
    public void FocusedEmptyTextField_KeepsPlaceholderFullyVisible()
    {
        var screen = new Ambystech.Elaris.UI.Rendering.Screen(30, 1, new VirtualTerminal(30, 1));
        var field = new TextField("New name") { Bounds = new Rectangle(0, 0, 20, 1) };
        field.OnFocus();

        field.Render(screen);

        string.Concat(Enumerable.Range(0, 8).Select(x => screen.GetCell(x, 0).Character)).Should().Be("New name");
        screen.GetCell(0, 0).Background.ToArgb().Should().Be(field.ForegroundColor.ToArgb(), "the cursor is still shown, inverted");
    }

    [Fact]
    public void Wrap_BreaksOnWords_AndSplitsLongWords()
    {
        MessageBox.Wrap("the quick brown fox jumps", 10).Should().Equal("the quick", "brown fox", "jumps");
        MessageBox.Wrap("abcdefghijklmno", 6).Should().Equal("abcdef", "ghijkl", "mno");
        MessageBox.Wrap("line one\nline two", 20).Should().Equal("line one", "line two");
    }

    [Theory]
    [InlineData(MessageBoxButtons.OK, new[] { "OK" })]
    [InlineData(MessageBoxButtons.OKCancel, new[] { "OK", "Cancel" })]
    [InlineData(MessageBoxButtons.YesNo, new[] { "Yes", "No" })]
    [InlineData(MessageBoxButtons.YesNoCancel, new[] { "Yes", "No", "Cancel" })]
    public void MessageBox_HasRequestedButtons(MessageBoxButtons buttons, string[] expected)
    {
        var dialog = MessageBox.Create("Hello", "Title", buttons);

        dialog.Children.OfType<Button>().Select(b => b.Text).Should().Equal(expected);
        dialog.InitialFocus.Should().BeSameAs(dialog.Children.OfType<Button>().First());
    }

    [Theory]
    [InlineData(MessageBoxButtons.OK, DialogResult.OK)]
    [InlineData(MessageBoxButtons.OKCancel, DialogResult.Cancel)]
    [InlineData(MessageBoxButtons.YesNo, DialogResult.No)]
    [InlineData(MessageBoxButtons.YesNoCancel, DialogResult.Cancel)]
    public async Task MessageBox_Escape_GivesCancelOrLastButton(MessageBoxButtons buttons, DialogResult expected)
    {
        var (app, _, _, _) = CreateApp();
        var task = MessageBox.Show(app, "Message", "Title", buttons);

        app.HandleKey(Key(ConsoleKey.Escape));

        (await task.WaitAsync(Timeout)).Should().Be(expected);
    }

    [Fact]
    public async Task MessageBox_ArrowKeysMoveBetweenButtons_EnterChooses()
    {
        var (app, _, _, _) = CreateApp();
        var task = MessageBox.Show(app, "Save changes?", "Close", MessageBoxButtons.YesNoCancel);

        app.HandleKey(Key(ConsoleKey.RightArrow));
        app.HandleKey(Key(ConsoleKey.Enter, ch: '\r'));

        (await task.WaitAsync(Timeout)).Should().Be(DialogResult.No);
    }

    [Fact]
    public void MessageBox_SizesToContent_AndKeepsButtonsInside()
    {
        var (app, _, _, _) = CreateApp();
        var dialog = MessageBox.Create(new string('x', 100), "T", MessageBoxButtons.YesNoCancel);

        app.ShowDialog(dialog);

        dialog.Width.Should().Be(56 + 4, "long text wraps at 56 columns");
        dialog.Height.Should().Be(2 + 7, "two wrapped lines plus border, spacing and buttons");
        foreach (var button in dialog.Children.OfType<Button>())
        {
            button.Bounds.Left.Should().BeGreaterThan(dialog.Bounds.Left);
            button.Bounds.Right.Should().BeLessThan(dialog.Bounds.Right);
            button.Bounds.Bottom.Should().BeLessThan(dialog.Bounds.Bottom);
        }
    }

    [Fact]
    public async Task Run_DrawsDialogAboveContent_AndClosesWithKeys()
    {
        var terminal = new VirtualTerminal(60, 16);
        var app = new Application(terminal) { TargetFps = 200 };
        var root = new Container();
        root.Add(new Label(new string('#', 60)) { Bounds = new Rectangle(0, 7, 60, 1) });

        var run = Task.Factory.StartNew(() => app.Run(root), TaskCreationOptions.LongRunning);
        var result = MessageBox.Show(app, "Overwrite the file?", "Save", MessageBoxButtons.YesNo);

        var deadline = DateTime.UtcNow + Timeout;
        while (!terminal.GetText().Contains("Overwrite") && DateTime.UtcNow < deadline)
            Thread.Sleep(5);

        var text = terminal.GetText();
        text.Should().Contain("Save").And.Contain("Overwrite the file?").And.Contain("Yes").And.Contain("No");
        terminal.GetLine(7).Should().StartWith("#").And.Contain(" ", "the dialog covers the middle of the row");

        terminal.EnqueueKey(ConsoleKey.Enter, '\r');
        (await result.WaitAsync(Timeout)).Should().Be(DialogResult.Yes);

        deadline = DateTime.UtcNow + Timeout;
        while (terminal.GetText().Contains("Overwrite") && DateTime.UtcNow < deadline)
            Thread.Sleep(5);
        terminal.GetLine(7).Should().Be(new string('#', 60), "the content behind is drawn again");

        app.Stop();
        run.Wait(Timeout).Should().BeTrue();
    }

    [Fact]
    public async Task Run_OpenDialog_IsCancelled_WhenApplicationStops()
    {
        var terminal = new VirtualTerminal(40, 12);
        var app = new Application(terminal);
        var task = MessageBox.Show(app, "Still open");
        app.Post(app.Stop);

        app.Run(new Container());

        var act = () => task.WaitAsync(Timeout);
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task Run_ShowDialogFromBackgroundThread_Works()
    {
        var terminal = new VirtualTerminal(40, 12);
        var app = new Application(terminal) { TargetFps = 200 };
        var run = Task.Factory.StartNew(() => app.Run(new Container()), TaskCreationOptions.LongRunning);
        await app.InvokeAsync(() => { }).WaitAsync(Timeout);

        var result = await Task.Run(async () =>
        {
            var pending = MessageBox.Show(app, "From another thread");
            await app.InvokeAsync(() => app.OpenDialogs.Should().ContainSingle()).WaitAsync(Timeout);
            terminal.EnqueueKey(ConsoleKey.Enter, '\r');
            return await pending.WaitAsync(Timeout);
        });

        result.Should().Be(DialogResult.OK);
        app.Stop();
        run.Wait(Timeout).Should().BeTrue();
    }
}
