using Ambystech.Elaris.UI.Widgets.Input;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class EditorTests
{
    [Fact]
    public void Editor_Should_Initialize_With_Empty_Text()
    {
        var editor = new Editor();

        editor.Text.Should().BeEmpty();
        editor.Lines.Count.Should().Be(1);
        editor.Lines[0].Should().BeEmpty();
    }

    [Fact]
    public void Editor_Should_Set_Text_Content()
    {
        var editor = new Editor();
        string testText = "Line 1\nLine 2\nLine 3";

        editor.Text = testText;

        editor.Text.Should().Be(testText);
        editor.Lines.Count.Should().Be(3);
        editor.Lines[0].Should().Be("Line 1");
        editor.Lines[1].Should().Be("Line 2");
        editor.Lines[2].Should().Be("Line 3");
    }

    [Fact]
    public void Editor_Should_Insert_Text_At_Cursor()
    {
        var editor = new Editor();
        editor.Text = "Hello";

        editor.InsertText(" World");

        editor.Text.Should().Be("Hello World");
        editor.CursorColumn.Should().Be(11);
    }

    [Fact]
    public void Editor_Should_Insert_Newline()
    {
        var editor = new Editor();
        editor.Text = "Line 1";

        editor.InsertText("\n");

        editor.Lines.Count.Should().Be(2);
        editor.Lines[0].Should().Be("Line 1");
        editor.Lines[1].Should().BeEmpty();
        editor.CursorLine.Should().Be(1);
        editor.CursorColumn.Should().Be(0);
    }

    [Fact]
    public void Editor_Should_Delete_Character()
    {
        var editor = new Editor();
        editor.Text = "Hello";
        editor.CursorColumn = 3;

        editor.DeleteChar();

        editor.Text.Should().Be("Helo");
        editor.CursorColumn.Should().Be(3);
    }

    [Fact]
    public void Editor_Should_Delete_Backspace()
    {
        var editor = new Editor();
        editor.Text = "Hello";
        editor.CursorColumn = 3;

        editor.DeleteBackspace();

        editor.Text.Should().Be("Helo");
        editor.CursorColumn.Should().Be(2);
    }

    [Fact]
    public void Editor_Should_Move_Cursor()
    {
        var editor = new Editor();
        editor.Text = "Line 1\nLine 2\nLine 3";

        editor.MoveCursor(1, 3);

        editor.CursorLine.Should().Be(1);
        editor.CursorColumn.Should().Be(3);
    }

    [Fact]
    public void Editor_Should_Select_All()
    {
        var editor = new Editor();
        editor.Text = "Line 1\nLine 2";

        editor.SelectAll();

        editor.SelectionStart.Should().NotBeNull();
        editor.SelectionEnd.Should().NotBeNull();
    }

    [Fact]
    public void Editor_Should_Undo_Operation()
    {
        var editor = new Editor();
        editor.Text = "Hello";

        editor.InsertText(" World");
        editor.Undo();

        editor.Text.Should().Be("Hello");
    }

    [Fact]
    public void Editor_Should_Redo_Operation()
    {
        var editor = new Editor();
        editor.Text = "Hello";

        editor.InsertText(" World");
        editor.Undo();
        editor.Redo();

        editor.Text.Should().Be("Hello World");
    }

    [Fact]
    public void Editor_Should_Get_Line()
    {
        var editor = new Editor();
        editor.Text = "Line 1\nLine 2\nLine 3";

        editor.GetLine(1).Should().Be("Line 2");
    }

    [Fact]
    public void Editor_Should_Set_Line()
    {
        var editor = new Editor();
        editor.Text = "Line 1\nLine 2\nLine 3";

        editor.SetLine(1, "Modified Line");

        editor.Lines[1].Should().Be("Modified Line");
    }

    [Fact]
    public void Editor_Should_Be_Focusable()
    {
        var editor = new Editor();

        editor.IsFocusable.Should().BeTrue();
    }

    [Fact]
    public void Editor_Should_Handle_Empty_Text()
    {
        var editor = new Editor();
        editor.Text = "";

        editor.Lines.Count.Should().Be(1);
        editor.Lines[0].Should().BeEmpty();
    }

    [Fact]
    public void Editor_Should_Clamp_Cursor_To_Bounds()
    {
        var editor = new Editor();
        editor.Text = "Hello";

        editor.CursorLine = 10;
        editor.CursorColumn = 100;

        editor.CursorLine.Should().Be(0);
        editor.CursorColumn.Should().Be(5);
    }
}

