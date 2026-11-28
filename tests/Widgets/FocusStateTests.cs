using System.Drawing;
using Ambystech.Elaris.UI.Widgets;
using Ambystech.Elaris.UI.Widgets.Data;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using Ambystech.Elaris.UI.Widgets.Menu;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class FocusStateTests
{
    public static TheoryData<string, Func<Widget>> FocusableWidgets => new()
    {
        { nameof(Button), () => new Button("OK") },
        { nameof(Checkbox), () => new Checkbox("Check") },
        { nameof(TextField), () => new TextField() },
        { nameof(AutoCompleteTextField), () => new AutoCompleteTextField() },
        { nameof(ListBox), () => new ListBox() },
        { nameof(TextView), () => new TextView() },
        { nameof(Editor), () => new Editor() },
        { nameof(CodeEditor), () => new CodeEditor() },
        { nameof(TreeView), () => new TreeView() },
        { nameof(Table), () => new Table() },
        { nameof(MenuBar), () => new MenuBar() },
        { nameof(TabContainer), () => new TabContainer() },
        { nameof(Panel), () => new Panel("Panel") },
    };

    [Theory]
    [MemberData(nameof(FocusableWidgets))]
    public void HasFocus_FollowsOnFocusAndOnBlur(string name, Func<Widget> create)
    {
        var widget = create();

        widget.HasFocus.Should().BeFalse($"{name} starts unfocused");

        widget.OnFocus();
        widget.HasFocus.Should().BeTrue($"{name} should report focus after OnFocus");

        widget.OnBlur();
        widget.HasFocus.Should().BeFalse($"{name} should report no focus after OnBlur");
    }

    [Fact]
    public void HasFocus_FollowsApplicationFocusChanges()
    {
        var root = new Container();
        var first = new Button("First");
        var second = new TextField();
        root.Add(first);
        root.Add(second);

        var app = new Application();
        app.AttachRoot(root, new Rectangle(0, 0, 80, 24));

        first.HasFocus.Should().BeTrue();
        second.HasFocus.Should().BeFalse();

        app.FocusNext();

        first.HasFocus.Should().BeFalse();
        second.HasFocus.Should().BeTrue();
    }

    [Fact]
    public void HasFocus_IsFalse_ForNonFocusableWidget()
    {
        var label = new Label("text");

        label.OnFocus();

        label.HasFocus.Should().BeFalse();
    }
}
