using System.Drawing;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Terminal;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Rendering;

public class TransparencyTests
{
    private static readonly Color Panel = Color.FromArgb(255, 10, 60, 90);

    private static Screen CreateFilledScreen()
    {
        var screen = new Screen(10, 3, new VirtualTerminal(10, 3));
        screen.FillRectangle(new Rectangle(0, 0, 10, 2), ' ', Color.White, Panel);
        return screen;
    }

    [Fact]
    public void WriteText_WithTransparentBackground_KeepsBackgroundUnderneath()
    {
        var screen = CreateFilledScreen();

        screen.WriteText(1, 0, "hi", Color.Yellow, Color.Transparent);

        screen.GetCell(1, 0).Background.ToArgb().Should().Be(Panel.ToArgb());
        screen.GetCell(1, 0).Foreground.Should().Be(Color.Yellow);
    }

    [Fact]
    public void SetCell_AndDrawRectangle_WithTransparentBackground_KeepBackground()
    {
        var screen = CreateFilledScreen();

        screen.SetCell(5, 1, new Cell('x', Color.White, Color.Transparent));
        screen.DrawRectangle(new Rectangle(0, 0, 4, 2), Color.White, Color.Transparent);

        screen.GetCell(5, 1).Background.ToArgb().Should().Be(Panel.ToArgb());
        screen.GetCell(0, 0).Background.ToArgb().Should().Be(Panel.ToArgb());
    }

    [Fact]
    public void OpaqueBackground_StillReplaces()
    {
        var screen = CreateFilledScreen();

        screen.WriteText(1, 0, "hi", Color.White, Color.Red);

        screen.GetCell(1, 0).Background.Should().Be(Color.Red);
    }

    [Fact]
    public void TransparentBackground_OnUnfilledCell_StaysTransparent()
    {
        var screen = CreateFilledScreen();

        screen.WriteText(0, 2, "x", Color.White, Color.Transparent);

        screen.GetCell(0, 2).Background.A.Should().Be(0, "row 2 was never filled, so the terminal's background shows");
    }
}
