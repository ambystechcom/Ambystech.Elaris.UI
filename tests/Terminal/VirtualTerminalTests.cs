using System.Drawing;
using Ambystech.Elaris.UI.Terminal;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Terminal;

public class VirtualTerminalTests
{
    [Fact]
    public void Write_PlacesTextAtCursorPosition()
    {
        var terminal = new VirtualTerminal(10, 3);

        terminal.Write("\x1b[2;3Hhi");

        terminal.GetLine(1).Should().Be("  hi");
        terminal.GetText().Should().Be("\n  hi");
    }

    [Fact]
    public void Write_DecodesTrueColorAndAttributes()
    {
        var terminal = new VirtualTerminal(10, 1);

        terminal.Write("\x1b[0;1;3;4;38;2;1;2;3;48;2;4;5;6mx\x1b[0my");

        var x = terminal.GetCell(0, 0);
        x.Foreground.ToArgb().Should().Be(Color.FromArgb(255, 1, 2, 3).ToArgb());
        x.Background.ToArgb().Should().Be(Color.FromArgb(255, 4, 5, 6).ToArgb());
        x.Bold.Should().BeTrue();
        x.Italic.Should().BeTrue();
        x.Underline.Should().BeTrue();

        var y = terminal.GetCell(1, 0);
        y.Foreground.Should().Be(Color.Empty);
        y.Background.A.Should().Be(0);
        y.Bold.Should().BeFalse();
    }

    [Fact]
    public void Write_HandlesSequencesSplitAcrossCalls()
    {
        var terminal = new VirtualTerminal(10, 2);

        terminal.Write("\x1b[2");
        terminal.Write(";4");
        terminal.Write("Hz");

        terminal.GetCell(3, 1).Character.Should().Be('z');
    }

    [Fact]
    public void AlternateScreen_KeepsMainScreenContent()
    {
        var terminal = new VirtualTerminal(10, 1);
        terminal.Write("main");

        terminal.Write("\x1b[?1049h\x1b[1;1Halt");
        terminal.IsAlternateScreen.Should().BeTrue();
        terminal.GetLine(0).Should().Be("alt");

        terminal.Write("\x1b[?1049l");
        terminal.IsAlternateScreen.Should().BeFalse();
        terminal.GetLine(0).Should().Be("main");
    }

    [Fact]
    public void ClearScreen_AndClearLine()
    {
        var terminal = new VirtualTerminal(10, 2);
        terminal.Write("abcdef\x1b[2;1Hghij");

        terminal.Write("\x1b[1;3H\x1b[K");
        terminal.GetLine(0).Should().Be("ab");

        terminal.Write("\x1b[2J");
        terminal.GetText().Should().BeEmpty();
    }

    [Fact]
    public void CursorVisibility_IsTracked()
    {
        var terminal = new VirtualTerminal();

        terminal.Write("\x1b[?25l");
        terminal.IsCursorVisible.Should().BeFalse();

        terminal.Write("\x1b[?25h");
        terminal.IsCursorVisible.Should().BeTrue();
    }

    [Fact]
    public void EnqueueText_ProducesKeysInOrder()
    {
        var terminal = new VirtualTerminal();

        terminal.EnqueueText("Hi 1");

        var keys = new List<ConsoleKeyInfo>();
        while (terminal.TryReadKey(out var key))
            keys.Add(key);

        keys.Select(k => k.KeyChar).Should().Equal('H', 'i', ' ', '1');
        keys.Select(k => k.Key).Should().Equal(ConsoleKey.H, ConsoleKey.I, ConsoleKey.Spacebar, ConsoleKey.D1);
        keys[0].Modifiers.Should().Be(ConsoleModifiers.Shift);
    }

    [Fact]
    public void TakeOutput_ReturnsRawOutputSinceLastCall()
    {
        var terminal = new VirtualTerminal();
        terminal.Write("a");
        terminal.Write("\x1b[0mb");

        terminal.TakeOutput().Should().Be("a\x1b[0mb");
        terminal.TakeOutput().Should().BeEmpty();
    }
}
