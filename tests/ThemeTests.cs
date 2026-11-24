using System.Drawing;
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

public class ThemeTests
{
    private static Color C(int n) => Color.FromArgb(255, n, n + 1, n + 2);

    /// <summary>A theme where every role has a different color, to check which role each property uses.</summary>
    private static readonly Theme Probe = new()
    {
        Name = "Probe",
        Foreground = C(10),
        Background = C(20),
        MutedForeground = C(30),
        Border = C(40),
        Accent = C(50),
        AccentForeground = C(60),
        Selection = C(70),
        SelectionForeground = C(80),
        Surface = C(90),
        SurfaceForeground = C(100),
        SurfaceAlt = C(110),
        InputBackground = C(120),
        Track = C(130),
        Success = C(140),
        Warning = C(150),
    };

    public static TheoryData<string, Func<Widget>, Func<Widget, Color>, Func<Theme, Color>> ThemedProperties => new()
    {
        { "Widget.ForegroundColor", () => new Label("x"), w => w.ForegroundColor, t => t.Foreground },
        { "Widget.BackgroundColor (transparent)", () => new Label("x"), w => w.BackgroundColor, _ => Color.Transparent },
        { "Button.BackgroundColor", () => new Button(), w => w.BackgroundColor, t => t.Surface },
        { "Button.ForegroundColor", () => new Button(), w => w.ForegroundColor, t => t.SurfaceForeground },
        { "Button.PressedBackgroundColor", () => new Button(), w => ((Button)w).PressedBackgroundColor, t => t.SurfaceAlt },
        { "Button.PressedForegroundColor", () => new Button(), w => ((Button)w).PressedForegroundColor, t => t.SurfaceForeground },
        { "Button.FocusedBackgroundColor", () => new Button(), w => ((Button)w).FocusedBackgroundColor, t => t.Accent },
        { "Button.FocusedForegroundColor", () => new Button(), w => ((Button)w).FocusedForegroundColor, t => t.AccentForeground },
        { "Button.BorderColor", () => new Button(), w => ((Button)w).BorderColor, t => t.Border },
        { "Checkbox.BracketColor", () => new Checkbox(), w => ((Checkbox)w).BracketColor, t => t.Warning },
        { "Checkbox.CheckmarkColor", () => new Checkbox(), w => ((Checkbox)w).CheckmarkColor, t => t.Success },
        { "TextField.PlaceholderColor", () => new TextField(), w => ((TextField)w).PlaceholderColor, t => t.MutedForeground },
        { "AutoComplete.SuggestionBackgroundColor", () => new AutoCompleteTextField(), w => ((AutoCompleteTextField)w).SuggestionBackgroundColor, t => t.SurfaceAlt },
        { "AutoComplete.SuggestionForegroundColor", () => new AutoCompleteTextField(), w => ((AutoCompleteTextField)w).SuggestionForegroundColor, t => t.Foreground },
        { "AutoComplete.SelectionHighlightColor", () => new AutoCompleteTextField(), w => ((AutoCompleteTextField)w).SelectionHighlightColor, t => t.Selection },
        { "ListBox.SelectionBackgroundColor", () => new ListBox(), w => ((ListBox)w).SelectionBackgroundColor, t => t.Selection },
        { "ListBox.SelectionForegroundColor", () => new ListBox(), w => ((ListBox)w).SelectionForegroundColor, t => t.SelectionForeground },
        { "ListBox.ScrollbarTrackColor", () => new ListBox(), w => ((ListBox)w).ScrollbarTrackColor, t => t.Border },
        { "ListBox.ScrollbarThumbColor", () => new ListBox(), w => ((ListBox)w).ScrollbarThumbColor, t => t.Foreground },
        { "Editor.BackgroundColor", () => new Editor(), w => w.BackgroundColor, t => t.InputBackground },
        { "Editor.ForegroundColor", () => new Editor(), w => w.ForegroundColor, t => t.Foreground },
        { "Editor.LineNumberColor", () => new Editor(), w => ((Editor)w).LineNumberColor, t => t.MutedForeground },
        { "CodeEditor.BackgroundColor", () => new CodeEditor(), w => w.BackgroundColor, t => t.InputBackground },
        { "TreeView.SelectionBackgroundColor", () => new TreeView(), w => ((TreeView)w).SelectionBackgroundColor, t => t.Selection },
        { "TreeView.SelectionForegroundColor", () => new TreeView(), w => ((TreeView)w).SelectionForegroundColor, t => t.SelectionForeground },
        { "TreeView.ConnectorColor", () => new TreeView(), w => ((TreeView)w).ConnectorColor, t => t.MutedForeground },
        { "ProgressBar.FilledColor", () => new ProgressBar(), w => ((ProgressBar)w).FilledColor, t => t.Success },
        { "ProgressBar.UnfilledColor", () => new ProgressBar(), w => ((ProgressBar)w).UnfilledColor, t => t.Track },
        { "ProgressBar.FilledTextColor", () => new ProgressBar(), w => ((ProgressBar)w).FilledTextColor, t => t.Foreground },
        { "Table.HeaderForegroundColor", () => new Table(), w => ((Table)w).HeaderForegroundColor, t => t.SurfaceForeground },
        { "Table.HeaderBackgroundColor", () => new Table(), w => ((Table)w).HeaderBackgroundColor, t => t.Surface },
        { "Table.SelectedRowBackgroundColor", () => new Table(), w => ((Table)w).SelectedRowBackgroundColor, t => t.Selection },
        { "Table.SelectedRowForegroundColor", () => new Table(), w => ((Table)w).SelectedRowForegroundColor, t => t.SelectionForeground },
        { "Table.GridLineColor", () => new Table(), w => ((Table)w).GridLineColor, t => t.Border },
        { "Table.EmptyStateForegroundColor", () => new Table(), w => ((Table)w).EmptyStateForegroundColor, t => t.MutedForeground },
        { "TabContainer.ActiveTabBackgroundColor", () => new TabContainer(), w => ((TabContainer)w).ActiveTabBackgroundColor, t => t.Accent },
        { "TabContainer.ActiveTabForegroundColor", () => new TabContainer(), w => ((TabContainer)w).ActiveTabForegroundColor, t => t.AccentForeground },
        { "TabContainer.InactiveTabBackgroundColor", () => new TabContainer(), w => ((TabContainer)w).InactiveTabBackgroundColor, t => t.Surface },
        { "TabContainer.InactiveTabForegroundColor", () => new TabContainer(), w => ((TabContainer)w).InactiveTabForegroundColor, t => t.SurfaceForeground },
        { "TabContainer.TabBarBackgroundColor", () => new TabContainer(), w => ((TabContainer)w).TabBarBackgroundColor, t => t.SurfaceAlt },
        { "StatusBar.StatusBarBackground", () => new StatusBar(), w => ((StatusBar)w).StatusBarBackground, t => t.Track },
        { "StatusBar.StatusBarForeground", () => new StatusBar(), w => ((StatusBar)w).StatusBarForeground, t => t.Foreground },
        { "MenuBar.SelectedBackgroundColor", () => new MenuBar(), w => ((MenuBar)w).SelectedBackgroundColor, t => t.Selection },
        { "MenuBar.SelectedForegroundColor", () => new MenuBar(), w => ((MenuBar)w).SelectedForegroundColor, t => t.SelectionForeground },
        { "MenuBar.DropdownBackgroundColor", () => new MenuBar(), w => ((MenuBar)w).DropdownBackgroundColor, t => t.SurfaceAlt },
        { "MenuBar.DropdownForegroundColor", () => new MenuBar(), w => ((MenuBar)w).DropdownForegroundColor, t => t.Foreground },
        { "MenuBar.DropdownSelectedBackgroundColor", () => new MenuBar(), w => ((MenuBar)w).DropdownSelectedBackgroundColor, t => t.Selection },
        { "MenuBar.DropdownDisabledForegroundColor", () => new MenuBar(), w => ((MenuBar)w).DropdownDisabledForegroundColor, t => t.MutedForeground },
        { "MenuBar.DropdownBorderColor", () => new MenuBar(), w => ((MenuBar)w).DropdownBorderColor, t => t.Border },
        { "MenuDropdown.SelectedBackgroundColor", () => new MenuDropdown(), w => ((MenuDropdown)w).SelectedBackgroundColor, t => t.Selection },
        { "MenuDropdown.DisabledForegroundColor", () => new MenuDropdown(), w => ((MenuDropdown)w).DisabledForegroundColor, t => t.MutedForeground },
        { "MenuDropdown.BorderColor", () => new MenuDropdown(), w => ((MenuDropdown)w).BorderColor, t => t.Border },
        { "Frame.BorderColor (follows ForegroundColor)", () => new Frame(), w => ((Frame)w).BorderColor, t => t.Foreground },
    };

    [Theory]
    [MemberData(nameof(ThemedProperties))]
    public void Property_DefaultsToItsThemeRole(string name, Func<Widget> create, Func<Widget, Color> read, Func<Theme, Color> role)
    {
        var widget = create();
        widget.Theme = Probe;

        read(widget).ToArgb().Should().Be(role(Probe).ToArgb(), name);
    }

    [Theory]
    [MemberData(nameof(ThemedProperties))]
    public void Property_DefaultsToDarkTheme_WithoutATheme(string name, Func<Widget> create, Func<Widget, Color> read, Func<Theme, Color> role)
    {
        var widget = create();

        read(widget).ToArgb().Should().Be(role(Theme.Dark).ToArgb(), name);
    }

    [Fact]
    public void ExplicitColor_TakesPrecedenceOverTheme()
    {
        var button = new Button { BackgroundColor = Color.Red, Theme = Probe };

        button.BackgroundColor.Should().Be(Color.Red);
        button.FocusedBackgroundColor.ToArgb().Should().Be(Probe.Accent.ToArgb(), "only the color that was set is overridden");
    }

    [Fact]
    public void Theme_IsInheritedFromAncestors()
    {
        var root = new Container { Theme = Probe };
        var middle = new Container();
        var button = new Button();
        middle.Add(button);
        root.Add(middle);

        button.EffectiveTheme.Should().BeSameAs(Probe);
        button.BackgroundColor.ToArgb().Should().Be(Probe.Surface.ToArgb());
    }

    [Fact]
    public void NearestTheme_Wins()
    {
        var root = new Container { Theme = Theme.Light };
        var panel = new Container { Theme = Probe };
        var button = new Button();
        panel.Add(button);
        root.Add(panel);

        button.EffectiveTheme.Should().BeSameAs(Probe);
    }

    [Fact]
    public void EffectiveTheme_DefaultsToDark()
    {
        new Label("x").EffectiveTheme.Should().BeSameAs(Theme.Dark);
    }

    [Fact]
    public void ChangingTheme_RequestsRedraw()
    {
        var root = new Container();
        var label = new Label("x");
        root.Add(label);
        int requests = 0;
        root.RenderRequested = () => requests++;

        root.Theme = Theme.Light;

        requests.Should().Be(1);
    }

    [Fact]
    public void CustomTheme_CanBeDerivedWithWith()
    {
        var custom = Theme.Dark with { Accent = Color.Orange };

        custom.Accent.Should().Be(Color.Orange);
        custom.Selection.Should().Be(Theme.Dark.Selection);
        Theme.Dark.Accent.Should().NotBe(Color.Orange, "built-in themes are immutable");
    }

    [Fact]
    public void BuiltInThemes_HaveDistinctNames()
    {
        new[] { Theme.Dark.Name, Theme.Light.Name, Theme.HighContrast.Name }.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AutoCompletePopup_UsesOwnersTheme()
    {
        var screen = new Screen(30, 6, new VirtualTerminal(30, 6));
        var root = new Container { Bounds = new Rectangle(0, 0, 30, 6), Theme = Probe };
        var field = new AutoCompleteTextField { Bounds = new Rectangle(0, 0, 20, 1) };
        root.Add(field);

        field.ShowSuggestions(["one", "two"]);
        root.Render(screen);

        // Row 2 is the second (unselected) suggestion.
        screen.GetCell(1, 2).Background.ToArgb().Should().Be(Probe.SurfaceAlt.ToArgb());
        screen.GetCell(1, 1).Background.ToArgb().Should().Be(Probe.Selection.ToArgb(), "the first suggestion is selected");
    }

    [Fact]
    public void ApplicationTheme_AppliesToRoot_AndFillsBackground()
    {
        var terminal = new VirtualTerminal(30, 5);
        var app = new Application(terminal) { Theme = Theme.Light };
        var root = new Container();
        root.Add(new Label("hello") { Bounds = new Rectangle(1, 1, 10, 1) });

        Exception? error = null;
        var run = Task.Factory.StartNew(() =>
        {
            try { app.Run(root); } catch (Exception ex) { error = ex; }
        }, TaskCreationOptions.LongRunning);

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!terminal.GetText().Contains("hello") && DateTime.UtcNow < deadline)
            Thread.Sleep(5);

        root.Theme.Should().BeSameAs(Theme.Light);
        terminal.GetCell(20, 4).Background.ToArgb().Should().Be(Theme.Light.Background.ToArgb(), "empty areas are filled");
        terminal.GetCell(1, 1).Background.ToArgb().Should().Be(Theme.Light.Background.ToArgb(), "transparent label keeps the fill");
        terminal.GetCell(1, 1).Foreground.ToArgb().Should().Be(Theme.Light.Foreground.ToArgb());

        app.Stop();
        run.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ApplicationTheme_CanChangeWhileRunning()
    {
        var terminal = new VirtualTerminal(20, 3);
        var app = new Application(terminal) { Theme = Theme.Dark };
        var root = new Container();

        var run = Task.Factory.StartNew(() => app.Run(root), TaskCreationOptions.LongRunning);
        app.InvokeAsync(() => { }).Wait(TimeSpan.FromSeconds(10));

        app.InvokeAsync(() => app.Theme = Theme.HighContrast).Wait(TimeSpan.FromSeconds(10));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (terminal.GetCell(0, 0).Background.ToArgb() != Color.Black.ToArgb() && DateTime.UtcNow < deadline)
            Thread.Sleep(5);
        terminal.GetCell(0, 0).Background.ToArgb().Should().Be(Color.Black.ToArgb());

        app.Stop();
        run.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
    }
}
