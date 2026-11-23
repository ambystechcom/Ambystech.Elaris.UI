using System.Drawing;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Widgets;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Layout;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests;

public class ApplicationTests
{
    private static readonly Rectangle ScreenBounds = new(0, 0, 80, 24);

    private sealed class FocusableWidget(string name, Func<ConsoleKeyInfo, bool>? onKey = null) : Widget
    {
        public string Name { get; } = name;
        public List<ConsoleKeyInfo> Keys { get; } = [];
        public override bool IsFocusable => true;

        protected override void OnRender(Screen screen) { }

        protected internal override bool OnKeyPress(ConsoleKeyInfo key)
        {
            Keys.Add(key);
            return onKey?.Invoke(key) ?? false;
        }

        public override string ToString() => Name;
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool shift = false, bool alt = false, bool control = false, char ch = '\0')
        => new(ch, key, shift, alt, control);

    private static Application CreateApp(Widget root)
    {
        var app = new Application();
        app.AttachRoot(root, ScreenBounds);
        return app;
    }

    [Fact]
    public void GetFocusableWidgets_ExcludesNonFocusableDirectChildren()
    {
        var root = new Container();
        var label = new Label("text");
        var a = new FocusableWidget("a");
        root.Add(label);
        root.Add(a);

        var app = CreateApp(root);

        app.GetFocusableWidgets().Should().Equal(a);
    }

    [Fact]
    public void GetFocusableWidgets_ReturnsNestedWidgetsInTreeOrder()
    {
        var root = new Container();
        var inner = new Container();
        var a = new FocusableWidget("a");
        var b = new FocusableWidget("b");
        var c = new FocusableWidget("c");
        root.Add(a);
        root.Add(inner);
        inner.Add(b);
        root.Add(c);

        var app = CreateApp(root);

        app.GetFocusableWidgets().Should().Equal(a, b, c);
    }

    [Fact]
    public void GetFocusableWidgets_SkipsHiddenAndDisabledSubtrees()
    {
        var root = new Container();
        var hiddenParent = new Container { Visible = false };
        var hiddenChild = new FocusableWidget("hidden-child");
        var disabled = new FocusableWidget("disabled") { Enabled = false };
        var visible = new FocusableWidget("visible");
        hiddenParent.Add(hiddenChild);
        root.Add(hiddenParent);
        root.Add(disabled);
        root.Add(visible);

        var app = CreateApp(root);

        app.GetFocusableWidgets().Should().Equal(visible);
    }

    [Fact]
    public void AttachRoot_FocusesInitialFocusWidget_WhenFocusable()
    {
        var root = new Container();
        var a = new FocusableWidget("a");
        var b = new FocusableWidget("b");
        root.Add(a);
        root.Add(b);

        var app = new Application { InitialFocusWidget = b };
        app.AttachRoot(root, ScreenBounds);

        app.FocusedWidget.Should().BeSameAs(b);
    }

    [Fact]
    public void AltArrows_CycleFocusInBothDirections()
    {
        var root = new Container();
        var a = new FocusableWidget("a");
        var b = new FocusableWidget("b");
        root.Add(a);
        root.Add(b);
        var app = CreateApp(root);

        app.HandleKey(Key(ConsoleKey.RightArrow, alt: true));
        app.FocusedWidget.Should().BeSameAs(b);

        app.HandleKey(Key(ConsoleKey.RightArrow, alt: true));
        app.FocusedWidget.Should().BeSameAs(a);

        app.HandleKey(Key(ConsoleKey.LeftArrow, alt: true));
        app.FocusedWidget.Should().BeSameAs(b);

        a.Keys.Should().BeEmpty();
        b.Keys.Should().BeEmpty();
    }

    [Fact]
    public void FocusNext_IncludesWidgetsAddedAfterStart()
    {
        var root = new Container();
        var a = new FocusableWidget("a");
        root.Add(a);
        var app = CreateApp(root);

        var late = new FocusableWidget("late");
        root.Add(late);
        app.FocusNext();

        app.FocusedWidget.Should().BeSameAs(late);
    }

    [Fact]
    public void HandleKey_MovesFocus_WhenFocusedWidgetWasRemoved()
    {
        var root = new Container();
        var a = new FocusableWidget("a");
        var b = new FocusableWidget("b");
        root.Add(a);
        root.Add(b);
        var app = CreateApp(root);

        root.Remove(a);
        app.HandleKey(Key(ConsoleKey.X, ch: 'x'));

        app.FocusedWidget.Should().BeSameAs(b);
        b.Keys.Should().ContainSingle();
        a.Keys.Should().BeEmpty();
    }

    [Fact]
    public void Escape_StopsApplication_WhenFocusedWidgetDoesNotHandleIt()
    {
        var root = new Container();
        root.Add(new FocusableWidget("a"));
        var app = CreateApp(root);

        app.HandleKey(Key(ConsoleKey.Escape));

        app.StopRequested.Should().BeTrue();
    }

    [Fact]
    public void Escape_DoesNotStopApplication_WhenFocusedWidgetHandlesIt()
    {
        var root = new Container();
        var popupOwner = new FocusableWidget("popup", k => k.Key == ConsoleKey.Escape);
        root.Add(popupOwner);
        var app = CreateApp(root);

        app.HandleKey(Key(ConsoleKey.Escape));

        app.StopRequested.Should().BeFalse();
        popupOwner.Keys.Should().ContainSingle();
    }

    [Fact]
    public void QuitKey_CanBeDisabled()
    {
        var root = new Container();
        root.Add(new FocusableWidget("a"));
        var app = CreateApp(root);
        app.QuitKey = null;

        app.HandleKey(Key(ConsoleKey.Escape));

        app.StopRequested.Should().BeFalse();
    }

    [Fact]
    public void QuitKey_CanBeChanged()
    {
        var root = new Container();
        root.Add(new FocusableWidget("a"));
        var app = CreateApp(root);
        app.QuitKey = ConsoleKey.F10;

        app.HandleKey(Key(ConsoleKey.Escape));
        app.StopRequested.Should().BeFalse();

        app.HandleKey(Key(ConsoleKey.F10));
        app.StopRequested.Should().BeTrue();
    }

    [Fact]
    public void CtrlC_StopsApplication_BeforeReachingWidget()
    {
        var root = new Container();
        var a = new FocusableWidget("a", _ => true);
        root.Add(a);
        var app = CreateApp(root);

        app.HandleKey(Key(ConsoleKey.C, control: true));

        app.StopRequested.Should().BeTrue();
        a.Keys.Should().BeEmpty();
    }

    [Fact]
    public void CtrlC_IsPassedToWidget_WhenQuitOnCtrlCIsDisabled()
    {
        var root = new Container();
        var a = new FocusableWidget("a", _ => true);
        root.Add(a);
        var app = CreateApp(root);
        app.QuitOnCtrlC = false;

        app.HandleKey(Key(ConsoleKey.C, control: true));

        app.StopRequested.Should().BeFalse();
        a.Keys.Should().ContainSingle();
    }

    [Fact]
    public void Panel_IsFocusable()
    {
        new Panel("Settings").IsFocusable.Should().BeTrue();
    }
}
