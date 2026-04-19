using Ambystech.Elaris.UI.Widgets.Input;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class CodeEditorTests
{
    [Fact]
    public void CodeEditor_Should_Initialize_With_Default_Language()
    {
        var codeEditor = new CodeEditor();

        codeEditor.Language.Should().Be("csharp");
        codeEditor.SyntaxHighlighting.Should().BeTrue();
        codeEditor.ShowBracketMatching.Should().BeTrue();
    }

    [Fact]
    public void CodeEditor_Should_Set_Language()
    {
        var codeEditor = new CodeEditor();

        codeEditor.SetLanguage("javascript");

        codeEditor.Language.Should().Be("javascript");
    }

    [Fact]
    public void CodeEditor_Should_Fallback_To_CSharp_For_Invalid_Language()
    {
        var codeEditor = new CodeEditor();

        codeEditor.SetLanguage("invalid");

        codeEditor.Language.Should().Be("csharp");
    }

    [Fact]
    public void CodeEditor_Should_Get_Indentation_Level()
    {
        var codeEditor = new CodeEditor();
        codeEditor.Text = "    Indented line\n        More indented";

        codeEditor.GetIndentationLevel(0).Should().Be(4);
        codeEditor.GetIndentationLevel(1).Should().Be(8);
    }

    [Fact]
    public void CodeEditor_Should_Find_Matching_Bracket()
    {
        var codeEditor = new CodeEditor();
        codeEditor.Text = "function test() {\n    return true;\n}";
        codeEditor.Language = "javascript";

        var match = codeEditor.FindMatchingBracket(0, 16);

        match.Should().NotBeNull();
        match.Value.Y.Should().Be(2);
        match.Value.X.Should().Be(0);
    }

    [Fact]
    public void CodeEditor_Should_Return_Null_For_Invalid_Bracket_Position()
    {
        var codeEditor = new CodeEditor();
        codeEditor.Text = "function test() { }";

        var match = codeEditor.FindMatchingBracket(0, 5);

        match.Should().BeNull();
    }

    [Fact]
    public void CodeEditor_Should_Handle_Tab_Size()
    {
        var codeEditor = new CodeEditor();
        codeEditor.TabSize = 2;

        codeEditor.TabSize.Should().Be(2);
    }

    [Fact]
    public void CodeEditor_Should_Clamp_Tab_Size_To_Minimum()
    {
        var codeEditor = new CodeEditor();
        codeEditor.TabSize = 0;

        codeEditor.TabSize.Should().Be(1);
    }

    [Fact]
    public void CodeEditor_Should_Disable_Syntax_Highlighting()
    {
        var codeEditor = new CodeEditor();
        codeEditor.SyntaxHighlighting = false;

        codeEditor.SyntaxHighlighting.Should().BeFalse();
    }

    [Fact]
    public void CodeEditor_Should_Inherit_From_Editor()
    {
        var codeEditor = new CodeEditor();

        codeEditor.Should().BeAssignableTo<Editor>();
        codeEditor.IsFocusable.Should().BeTrue();
    }

    [Fact]
    public void CodeEditor_Should_Initialize_With_Default_Theme()
    {
        var codeEditor = new CodeEditor();

        codeEditor.Theme.Should().Be("default");
    }

    [Fact]
    public void CodeEditor_Should_Set_Theme()
    {
        var codeEditor = new CodeEditor();

        codeEditor.Theme = "default";
        codeEditor.Theme.Should().Be("default");
    }
}

