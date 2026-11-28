using System.Drawing;
using System.Text.RegularExpressions;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Terminal;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Rendering;

public class ScreenRenderTests
{
    private static (Screen screen, VirtualTerminal terminal) Create(int width = 20, int height = 5)
    {
        var terminal = new VirtualTerminal(width, height);
        var screen = new Screen(width, height, terminal);
        screen.Initialize();
        terminal.TakeOutput();
        return (screen, terminal);
    }

    private static int CountSgr(string output) => Regex.Matches(output, @"\x1b\[[0-9;]*m").Count;
    private static int CountCursorMoves(string output) => Regex.Matches(output, @"\x1b\[[0-9]+;[0-9]+H").Count;

    /// <summary>
    /// What the terminal shows for a cell from the back buffer: control characters become
    /// spaces and transparent colors become the terminal default.
    /// </summary>
    private static void AssertTerminalMatchesScreen(Screen screen, VirtualTerminal terminal)
    {
        for (int y = 0; y < screen.Height; y++)
        {
            for (int x = 0; x < screen.Width; x++)
            {
                var expected = screen.GetCell(x, y);
                var actual = terminal.GetCell(x, y);

                char expectedChar = char.IsControl(expected.Character) ? ' ' : expected.Character;
                actual.Character.Should().Be(expectedChar, $"character at ({x},{y})");

                int expectedFg = expected.Foreground.A == 0 ? 0 : expected.Foreground.ToArgb();
                int actualFg = actual.Foreground.A == 0 ? 0 : actual.Foreground.ToArgb();
                actualFg.Should().Be(expectedFg, $"foreground at ({x},{y})");

                int expectedBg = expected.Background.A == 0 ? 0 : expected.Background.ToArgb();
                int actualBg = actual.Background.A == 0 ? 0 : actual.Background.ToArgb();
                actualBg.Should().Be(expectedBg, $"background at ({x},{y})");

                actual.Bold.Should().Be(expected.Bold, $"bold at ({x},{y})");
                actual.Italic.Should().Be(expected.Italic, $"italic at ({x},{y})");
                actual.Underline.Should().Be(expected.Underline, $"underline at ({x},{y})");
            }
        }
    }

    [Fact]
    public void Initialize_EntersAlternateScreenAndHidesCursor()
    {
        var terminal = new VirtualTerminal(10, 3);
        var screen = new Screen(10, 3, terminal);

        screen.Initialize();

        terminal.IsInitialized.Should().BeTrue();
        terminal.IsAlternateScreen.Should().BeTrue();
        terminal.IsCursorVisible.Should().BeFalse();
    }

    [Fact]
    public void Shutdown_LeavesAlternateScreenAndShowsCursor()
    {
        var (screen, terminal) = Create();

        screen.Shutdown();

        terminal.IsInitialized.Should().BeFalse();
        terminal.IsAlternateScreen.Should().BeFalse();
        terminal.IsCursorVisible.Should().BeTrue();
    }

    [Fact]
    public void Render_DrawsTextWithColorsAndAttributes()
    {
        var (screen, terminal) = Create();
        var fg = Color.FromArgb(255, 10, 20, 30);
        var bg = Color.FromArgb(255, 200, 100, 50);

        screen.WriteText(2, 1, "Hello", fg, bg, bold: true, underline: true);
        screen.Render();

        terminal.GetLine(1).Should().Be("  Hello");
        var cell = terminal.GetCell(2, 1);
        cell.Foreground.ToArgb().Should().Be(fg.ToArgb());
        cell.Background.ToArgb().Should().Be(bg.ToArgb());
        cell.Bold.Should().BeTrue();
        cell.Underline.Should().BeTrue();
        cell.Italic.Should().BeFalse();
    }

    [Fact]
    public void Render_KeepsCursorHidden()
    {
        var (screen, terminal) = Create();

        screen.WriteText(0, 0, "x", Color.White, Color.Black);
        screen.Render();

        terminal.IsCursorVisible.Should().BeFalse();
    }

    [Fact]
    public void Render_WritesNothing_WhenNothingChanged()
    {
        var (screen, terminal) = Create();
        screen.WriteText(0, 0, "Hello", Color.White, Color.Black);
        screen.Render();
        terminal.TakeOutput();
        int flushes = terminal.FlushCount;

        screen.Clear();
        screen.WriteText(0, 0, "Hello", Color.White, Color.Black);
        screen.Render();

        terminal.TakeOutput().Should().BeEmpty();
        terminal.FlushCount.Should().Be(flushes);
    }

    [Fact]
    public void Render_TreatsNamedAndArgbColorsAsEqual()
    {
        var (screen, terminal) = Create();
        screen.WriteText(0, 0, "Hello", Color.White, Color.Black);
        screen.Render();
        terminal.TakeOutput();

        screen.Clear();
        screen.WriteText(0, 0, "Hello", Color.FromArgb(255, 255, 255, 255), Color.FromArgb(255, 0, 0, 0));
        screen.Render();

        terminal.TakeOutput().Should().BeEmpty();
    }

    [Fact]
    public void Render_WritesOneStyleAndOneMovePerRun_OfSameStyledCells()
    {
        var (screen, terminal) = Create();

        screen.WriteText(3, 2, "abcdef", Color.Red, Color.Blue);
        screen.Render();

        // The first frame also redraws the blank cells around the text.
        terminal.TakeOutput();

        screen.Clear();
        screen.WriteText(3, 2, "ABCDEF", Color.Red, Color.Blue);
        screen.Render();
        var output = terminal.TakeOutput();

        CountCursorMoves(output).Should().Be(1);
        CountSgr(output).Should().Be(2, "one style sequence for the run and one reset at the end");
        terminal.GetLine(2).Should().Be("   ABCDEF");
    }

    [Fact]
    public void Render_FirstFullFrame_IsCompact()
    {
        var (screen, terminal) = Create(80, 24);
        screen.FillRectangle(new Rectangle(0, 0, 80, 24), ' ', Color.White, Color.Black);

        screen.Render();
        var output = terminal.TakeOutput();

        // One move per row (rows wrap explicitly), one style, one reset.
        CountCursorMoves(output).Should().Be(24);
        CountSgr(output).Should().Be(2);
        output.Length.Should().BeLessThan(80 * 24 + 24 * 8 + 64);
    }

    [Fact]
    public void Render_MovesCursorExplicitly_AfterCharactersOfUnknownWidth()
    {
        var (screen, terminal) = Create();
        screen.WriteText(0, 0, "a中b", Color.White, Color.Black);
        screen.Render();
        terminal.TakeOutput();

        screen.Clear();
        screen.WriteText(0, 0, "A中B", Color.White, Color.Black);
        screen.Render();
        var output = terminal.TakeOutput();

        // 'A' and 'B' changed; '中' did not. Each changed cell needs its own move here.
        CountCursorMoves(output).Should().Be(2);
    }

    [Fact]
    public void Render_ReplacesControlCharacters()
    {
        var (screen, terminal) = Create();

        screen.WriteText(0, 0, "a\tb\nc\u001bd", Color.White, Color.Black);
        screen.Render();

        terminal.GetLine(0).Should().Be("a b c d");
        terminal.GetLine(1).Should().BeEmpty();
    }

    [Fact]
    public void Render_TransparentBackground_UsesTerminalDefault()
    {
        var (screen, terminal) = Create();

        screen.WriteText(0, 0, "x", Color.White, Color.Transparent);
        screen.Render();

        terminal.GetCell(0, 0).Background.A.Should().Be(0);
    }

    [Fact]
    public void Render_AfterResize_RedrawsEverything()
    {
        var (screen, terminal) = Create(10, 3);
        screen.WriteText(0, 0, "before", Color.White, Color.Black);
        screen.Render();

        terminal.Resize(12, 4);
        screen.Resize(12, 4);
        screen.WriteText(0, 3, "after", Color.White, Color.Black);
        screen.Render();

        AssertTerminalMatchesScreen(screen, terminal);
        terminal.GetLine(3).Should().Be("after");
    }

    [Fact]
    public void Invalidate_ClearsTerminalAndForcesRedraw()
    {
        var (screen, terminal) = Create();
        screen.WriteText(0, 0, "Hello", Color.White, Color.Black);
        screen.Render();

        screen.Invalidate();
        terminal.GetLine(0).Should().BeEmpty();

        screen.WriteText(0, 0, "Hello", Color.White, Color.Black);
        screen.Render();
        terminal.GetLine(0).Should().Be("Hello");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Render_RandomFrames_TerminalAlwaysMatchesScreen(int seed)
    {
        var random = new Random(seed);
        var (screen, terminal) = Create(30, 10);
        Color[] palette =
        [
            Color.White, Color.Black, Color.Red, Color.FromArgb(255, 255, 0, 0),
            Color.Transparent, Color.FromArgb(0, 1, 2, 3), Color.FromArgb(255, 40, 80, 120),
        ];
        const string chars = "ab XY─│┌┐▶中\té";

        for (int frame = 0; frame < 25; frame++)
        {
            screen.Clear();

            int operations = random.Next(1, 15);
            for (int i = 0; i < operations; i++)
            {
                var fg = palette[random.Next(palette.Length)];
                var bg = palette[random.Next(palette.Length)];
                int x = random.Next(-3, 32);
                int y = random.Next(-1, 11);

                switch (random.Next(3))
                {
                    case 0:
                        var text = new string(Enumerable.Range(0, random.Next(1, 12))
                            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
                        screen.WriteText(x, y, text, fg, bg,
                            bold: random.Next(2) == 0, italic: random.Next(2) == 0, underline: random.Next(2) == 0);
                        break;
                    case 1:
                        screen.FillRectangle(new Rectangle(x, y, random.Next(1, 10), random.Next(1, 5)), '.', fg, bg);
                        break;
                    default:
                        screen.DrawRectangle(new Rectangle(x, y, random.Next(2, 12), random.Next(2, 6)), fg, bg);
                        break;
                }
            }

            screen.Render();
            AssertTerminalMatchesScreen(screen, terminal);
        }
    }
}
