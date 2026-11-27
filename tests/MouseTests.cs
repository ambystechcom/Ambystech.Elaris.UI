using System.Drawing;
using Ambystech.Elaris.UI.Input;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Terminal;
using Ambystech.Elaris.UI.Widgets;
using Ambystech.Elaris.UI.Widgets.Data;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using Ambystech.Elaris.UI.Widgets.Menu;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests;

/// <summary>
/// Mouse routing in <see cref="Application"/> and mouse behavior of each widget.
/// Routing tests call <see cref="Application.HandleMouse"/> directly, without a console.
/// </summary>
public class MouseTests
{
    private static readonly Rectangle Screen80x24 = new(0, 0, 80, 24);

    private static MouseEvent Press(int x, int y) => new(x, y, MouseButton.Left, MouseEventKind.Press);
    private static MouseEvent Release(int x, int y) => new(x, y, MouseButton.Left, MouseEventKind.Release);
    private static MouseEvent Wheel(int x, int y, bool down) => new(x, y, MouseButton.None, down ? MouseEventKind.WheelDown : MouseEventKind.WheelUp);

    private static Application Attach(Widget root)
    {
        var app = new Application();
        app.AttachRoot(root, Screen80x24);
        return app;
    }

    private static void Click(Application app, int x, int y)
    {
        app.HandleMouse(Press(x, y));
        app.HandleMouse(Release(x, y));
    }

    private sealed class Recorder(string name) : Widget
    {
        public string Name { get; } = name;
        public List<MouseEvent> Events { get; } = [];
        public bool Handles { get; init; } = true;
        public bool Focusable { get; init; }
        public override bool IsFocusable => Focusable;
        protected override void OnRender(Screen screen) { }

        protected internal override bool OnMouse(MouseEvent e)
        {
            Events.Add(e);
            return Handles;
        }
    }

    // ---- Routing ----

    [Fact]
    public void HitTest_ReturnsTopmostChild()
    {
        var root = new Container();
        var below = new Recorder("below") { Bounds = new Rectangle(0, 0, 10, 5) };
        var above = new Recorder("above") { Bounds = new Rectangle(5, 0, 10, 5) };
        root.Add(below);
        root.Add(above);
        Attach(root);

        root.HitTest(new Point(6, 1)).Should().BeSameAs(above);
        root.HitTest(new Point(2, 1)).Should().BeSameAs(below);
        root.HitTest(new Point(50, 20)).Should().BeSameAs(root);
    }

    [Fact]
    public void HitTest_HigherZIndexWins()
    {
        var root = new Container();
        var high = new Recorder("high") { Bounds = new Rectangle(0, 0, 10, 5), ZIndex = 10 };
        var low = new Recorder("low") { Bounds = new Rectangle(0, 0, 10, 5) };
        root.Add(high);
        root.Add(low);
        Attach(root);

        root.HitTest(new Point(1, 1)).Should().BeSameAs(high);
    }

    [Fact]
    public void Add_OrdersByZIndex_StablyAmongEqualZIndex()
    {
        var root = new Container();
        var a = new Recorder("a");
        var top = new Recorder("top") { ZIndex = 5 };
        var b = new Recorder("b");
        var c = new Recorder("c");
        root.Add(a);
        root.Add(top);
        root.Add(b);
        root.Add(c);

        root.Children.Select(w => ((Recorder)w).Name).Should().Equal("a", "b", "c", "top");

        b.ZIndex = 5;
        root.Children.Select(w => ((Recorder)w).Name).Should().Equal("a", "c", "b", "top");
    }

    [Fact]
    public void HitTest_IgnoresHiddenWidgets_AndChildrenOutsideTheirClip()
    {
        var root = new Container();
        var frame = new Frame("F") { Bounds = new Rectangle(0, 0, 10, 5) };
        var overflowing = new Recorder("overflow") { Bounds = new Rectangle(2, 2, 30, 1) };
        var hidden = new Recorder("hidden") { Bounds = new Rectangle(40, 0, 5, 5), Visible = false };
        frame.Add(overflowing);
        root.Add(frame);
        root.Add(hidden);
        Attach(root);

        root.HitTest(new Point(3, 2)).Should().BeSameAs(overflowing);
        root.HitTest(new Point(20, 2)).Should().BeSameAs(root, "that part of the child is clipped away");
        root.HitTest(new Point(41, 1)).Should().BeSameAs(root);
    }

    [Fact]
    public void LeftPress_FocusesNearestFocusableAncestor()
    {
        var root = new Container();
        var first = new Button("first") { Bounds = new Rectangle(0, 0, 10, 3) };
        var panel = new Recorder("panel") { Bounds = new Rectangle(0, 5, 20, 5), Focusable = true, Handles = false };
        var inner = new Recorder("inner") { Bounds = new Rectangle(1, 6, 5, 1), Handles = false };
        panel.Add(inner);
        root.Add(first);
        root.Add(panel);
        var app = Attach(root);
        app.FocusedWidget.Should().BeSameAs(first);

        app.HandleMouse(Press(2, 6));

        app.FocusedWidget.Should().BeSameAs(panel);
    }

    [Fact]
    public void LeftPress_OnNonFocusableArea_KeepsFocus()
    {
        var root = new Container();
        var button = new Button("b") { Bounds = new Rectangle(0, 0, 10, 3) };
        root.Add(button);
        root.Add(new Label("text") { Bounds = new Rectangle(0, 10, 10, 1) });
        var app = Attach(root);

        app.HandleMouse(Press(1, 10));

        app.FocusedWidget.Should().BeSameAs(button);
    }

    [Fact]
    public void UnhandledEvents_GoToParent()
    {
        var root = new Container();
        var parent = new Recorder("parent") { Bounds = new Rectangle(0, 0, 20, 10) };
        var child = new Recorder("child") { Bounds = new Rectangle(1, 1, 5, 1), Handles = false };
        parent.Add(child);
        root.Add(parent);
        var app = Attach(root);

        app.HandleMouse(Press(2, 1));

        child.Events.Should().ContainSingle();
        parent.Events.Should().ContainSingle();
    }

    [Fact]
    public void AfterLeftPress_DragAndRelease_GoToTheSameWidget()
    {
        var root = new Container();
        var a = new Recorder("a") { Bounds = new Rectangle(0, 0, 5, 5) };
        var b = new Recorder("b") { Bounds = new Rectangle(10, 0, 5, 5) };
        root.Add(a);
        root.Add(b);
        var app = Attach(root);

        app.HandleMouse(Press(1, 1));
        app.HandleMouse(new MouseEvent(11, 1, MouseButton.Left, MouseEventKind.Drag));
        app.HandleMouse(Release(11, 1));
        app.HandleMouse(Press(11, 1));

        a.Events.Select(e => e.Kind).Should().Equal(MouseEventKind.Press, MouseEventKind.Drag, MouseEventKind.Release);
        b.Events.Select(e => e.Kind).Should().Equal(MouseEventKind.Press);
    }

    [Fact]
    public void DoubleClick_IsCounted_OnlyAtSamePositionAndWithinTime()
    {
        var root = new Container();
        var r = new Recorder("r") { Bounds = new Rectangle(0, 0, 20, 5) };
        root.Add(r);
        var app = Attach(root);
        app.DoubleClickTime = TimeSpan.FromSeconds(10);

        Click(app, 1, 1);
        Click(app, 1, 1);
        Click(app, 1, 1);
        Click(app, 2, 1);

        r.Events.Where(e => e.Kind == MouseEventKind.Press).Select(e => e.ClickCount).Should().Equal(1, 2, 3, 1);
    }

    [Fact]
    public void DoubleClick_NotCounted_WhenTooSlow()
    {
        var root = new Container();
        var r = new Recorder("r") { Bounds = new Rectangle(0, 0, 20, 5) };
        root.Add(r);
        var app = Attach(root);
        app.DoubleClickTime = TimeSpan.Zero;

        Click(app, 1, 1);
        Thread.Sleep(20);
        Click(app, 1, 1);

        r.Events.Where(e => e.Kind == MouseEventKind.Press).Select(e => e.ClickCount).Should().Equal(1, 1);
    }

    [Fact]
    public void DisabledWidget_DoesNotReceiveEvents()
    {
        var root = new Container();
        var parent = new Recorder("parent") { Bounds = new Rectangle(0, 0, 20, 10) };
        var child = new Recorder("child") { Bounds = new Rectangle(1, 1, 5, 1) };
        parent.Add(child);
        root.Add(parent);
        child.Enabled = false;
        var app = Attach(root);

        app.HandleMouse(Press(2, 1));

        child.Events.Should().BeEmpty();
    }

    // ---- Widgets ----

    [Fact]
    public void Button_ClickInside_RaisesClick()
    {
        var root = new Container();
        var button = new Button("OK") { Bounds = new Rectangle(0, 0, 10, 3) };
        int clicks = 0;
        button.Click += () => clicks++;
        root.Add(button);
        var app = Attach(root);

        Click(app, 3, 1);

        clicks.Should().Be(1);
    }

    [Fact]
    public void Button_ReleaseOutside_DoesNotClick()
    {
        var root = new Container();
        var button = new Button("OK") { Bounds = new Rectangle(0, 0, 10, 3) };
        int clicks = 0, released = 0;
        button.Click += () => clicks++;
        button.Released += () => released++;
        root.Add(button);
        var app = Attach(root);

        app.HandleMouse(Press(3, 1));
        app.HandleMouse(Release(40, 10));

        clicks.Should().Be(0);
        released.Should().Be(1, "the button still returns to its normal look");
    }

    [Fact]
    public void Checkbox_Click_Toggles()
    {
        var root = new Container();
        var checkbox = new Checkbox("c") { Bounds = new Rectangle(0, 0, 10, 1) };
        root.Add(checkbox);
        var app = Attach(root);

        Click(app, 1, 0);

        checkbox.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void ListBox_ClickSelectsRow_DoubleClickAndEnterActivate()
    {
        var root = new Container();
        var list = new ListBox { Bounds = new Rectangle(0, 2, 20, 3) };
        foreach (var item in new[] { "a", "b", "c", "d", "e" })
            list.AddItem(item);
        var activated = new List<int>();
        list.ItemActivated += activated.Add;
        root.Add(list);
        var app = Attach(root);
        app.DoubleClickTime = TimeSpan.FromSeconds(10);

        Click(app, 1, 3);
        list.SelectedIndex.Should().Be(1);

        Click(app, 1, 3);
        activated.Should().Equal(1);

        list.OnKeyPress(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)).Should().BeTrue();
        activated.Should().Equal(1, 1);
    }

    [Fact]
    public void ListBox_ClickAccountsForScrolling()
    {
        var root = new Container();
        var list = new ListBox { Bounds = new Rectangle(0, 0, 20, 3) };
        for (int i = 0; i < 10; i++)
            list.AddItem($"item {i}");
        root.Add(list);
        var app = Attach(root);
        list.SelectedIndex = 9; // scrolls so items 7..9 are visible

        Click(app, 1, 0);

        list.SelectedIndex.Should().Be(7);
    }

    [Fact]
    public void ListBox_Wheel_MovesSelection()
    {
        var root = new Container();
        var list = new ListBox { Bounds = new Rectangle(0, 0, 20, 3) };
        for (int i = 0; i < 10; i++)
            list.AddItem($"item {i}");
        root.Add(list);
        var app = Attach(root);
        list.SelectedIndex = 0;

        app.HandleMouse(Wheel(1, 1, down: true));

        list.SelectedIndex.Should().Be(3);
    }

    [Fact]
    public void TextField_Click_MovesCursor()
    {
        var root = new Container();
        var field = new TextField { Bounds = new Rectangle(5, 0, 20, 1), Text = "hello world" };
        root.Add(field);
        var app = Attach(root);

        Click(app, 8, 0);
        field.OnKeyPress(new ConsoleKeyInfo('X', ConsoleKey.X, true, false, false));

        field.Text.Should().Be("helXlo world");
    }

    [Fact]
    public void TextField_ClickPastEnd_PutsCursorAtEnd()
    {
        var root = new Container();
        var field = new TextField { Bounds = new Rectangle(0, 0, 20, 1), Text = "hi" };
        root.Add(field);
        var app = Attach(root);

        Click(app, 15, 0);
        field.OnKeyPress(new ConsoleKeyInfo('!', ConsoleKey.NoName, false, false, false));

        field.Text.Should().Be("hi!");
    }

    [Fact]
    public void TabContainer_ClickOnTitle_ActivatesTab()
    {
        var root = new Container();
        var tabs = new TabContainer { Bounds = new Rectangle(0, 0, 60, 10) };
        tabs.AddTab("One");
        tabs.AddTab("Two");
        tabs.AddTab("Three");
        root.Add(tabs);
        var app = Attach(root);

        // Titles are drawn as "[ One ]" from x = 1, then "[ Two ]" from x = 9, "[ Three ]" from x = 17.
        Click(app, 10, 0);
        tabs.ActiveTabIndex.Should().Be(1);

        Click(app, 20, 0);
        tabs.ActiveTabIndex.Should().Be(2);
    }

    [Fact]
    public void MenuBar_ClickTitleOpensMenu_ClickItemInvokesIt()
    {
        var root = new Container();
        var menuBar = new MenuBar { Bounds = new Rectangle(0, 0, 80, 1) };
        string? invoked = null;
        menuBar.AddMenu("File", new MenuItem("New", () => invoked = "New"), new MenuItem("Open", () => invoked = "Open"));
        menuBar.AddMenu("Edit", new MenuItem("Copy", () => invoked = "Copy"));
        root.Add(menuBar);
        var app = Attach(root);

        Click(app, 2, 0); // " File " starts at x = 1
        root.Children.OfType<MenuDropdown>().Should().ContainSingle("the File menu is open");

        Click(app, 3, 2); // second item: "Open" on row 2
        invoked.Should().Be("Open");
        root.Children.OfType<MenuDropdown>().Should().BeEmpty("the menu closes after choosing an item");
    }

    [Fact]
    public void MenuBar_ClickOpenTitleAgain_ClosesMenu()
    {
        var root = new Container();
        var menuBar = new MenuBar { Bounds = new Rectangle(0, 0, 80, 1) };
        menuBar.AddMenu("File", new MenuItem("New"));
        root.Add(menuBar);
        var app = Attach(root);

        Click(app, 2, 0);
        Click(app, 2, 0);

        root.Children.OfType<MenuDropdown>().Should().BeEmpty();
    }

    [Fact]
    public void Table_ClickSelectsRow_DoubleClickActivates()
    {
        var root = new Container();
        var table = new Table { Bounds = new Rectangle(0, 0, 40, 10) };
        table.AddColumn("Name");
        var r0 = table.AddRow("zero");
        var r1 = table.AddRow("one");
        TableRow? activated = null;
        table.RowActivated += row => activated = row;
        root.Add(table);
        var app = Attach(root);
        app.DoubleClickTime = TimeSpan.FromSeconds(10);

        Click(app, 2, 3); // header + separator take rows 0-1; row 3 is the second data row
        table.SelectedRowIndex.Should().Be(1);

        Click(app, 2, 3);
        activated.Should().BeSameAs(r1);
    }

    [Fact]
    public void TreeView_ClickSelects_DoubleClickExpands()
    {
        var root = new Container();
        var tree = new TreeView { Bounds = new Rectangle(0, 0, 40, 10) };
        var parent = new TreeNode("parent");
        parent.AddChild(new TreeNode("child"));
        var other = new TreeNode("other");
        tree.RootNodes.Add(parent);
        tree.RootNodes.Add(other);
        root.Add(tree);
        var app = Attach(root);
        app.DoubleClickTime = TimeSpan.FromSeconds(10);

        Click(app, 3, 1);
        tree.SelectedNode.Should().BeSameAs(other);

        Click(app, 3, 0);
        Click(app, 3, 0);
        tree.SelectedNode.Should().BeSameAs(parent);
        parent.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void Editor_ClickMovesCursor()
    {
        var root = new Container();
        var editor = new Editor { Bounds = new Rectangle(0, 0, 40, 10), Text = "first line\nsecond line" };
        root.Add(editor);
        var app = Attach(root);
        int gutter = editor.ShowLineNumbers ? 2 : 0; // 1 digit + 1 space for two lines

        Click(app, gutter + 3, 1);

        editor.CursorLine.Should().Be(1);
        editor.CursorColumn.Should().Be(3);
    }

    [Fact]
    public void AutoComplete_ClickOnSuggestion_AcceptsIt()
    {
        var root = new Container();
        var field = new AutoCompleteTextField { Bounds = new Rectangle(0, 0, 20, 1) };
        root.Add(field);
        var app = Attach(root);
        string? selected = null;
        field.SuggestionSelected += s => selected = s;
        field.ShowSuggestions(["apple", "apricot"]);
        var screen = new Screen(80, 24, new VirtualTerminal(80, 24));
        root.Render(screen); // positions the popup below the field

        Click(app, 2, 2);

        selected.Should().Be("apricot");
        field.Text.Should().Be("apricot");
    }

    [Fact]
    public void Panel_ClickOnHeader_TogglesCollapsed()
    {
        var root = new Container();
        var panel = new Panel("Settings") { Bounds = new Rectangle(0, 0, 30, 6) };
        root.Add(panel);
        var app = Attach(root);

        Click(app, 5, 0);

        panel.Collapsed.Should().BeTrue();
    }

    // ---- Running application ----

    [Fact]
    public void Run_TurnsMouseReportingOnAndOff()
    {
        var terminal = new VirtualTerminal(40, 10);
        var app = new Application(terminal) { TargetFps = 200 };
        bool wasOn = false;
        app.Post(() =>
        {
            wasOn = terminal.IsMouseReportingEnabled;
            app.Stop();
        });

        app.Run(new Container());

        wasOn.Should().BeTrue();
        terminal.IsMouseReportingEnabled.Should().BeFalse();
    }

    [Fact]
    public void Run_MouseEnabledFalse_DoesNotTurnReportingOn()
    {
        var terminal = new VirtualTerminal(40, 10);
        var app = new Application(terminal) { MouseEnabled = false };
        bool wasOn = true;
        app.Post(() =>
        {
            wasOn = terminal.IsMouseReportingEnabled;
            app.Stop();
        });

        app.Run(new Container());

        wasOn.Should().BeFalse();
    }

    [Fact]
    public void Run_QueuedClick_ClicksButton()
    {
        var terminal = new VirtualTerminal(40, 10);
        var app = new Application(terminal) { TargetFps = 200 };
        var root = new Container();
        var button = new Button("Go") { Bounds = new Rectangle(2, 2, 10, 3) };
        button.Click += app.Stop;
        root.Add(button);

        terminal.EnqueueClick(5, 3);
        var run = Task.Factory.StartNew(() => app.Run(root), TaskCreationOptions.LongRunning);

        run.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue("clicking the button stops the app");
    }
}
