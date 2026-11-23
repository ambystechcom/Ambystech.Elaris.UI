using System.Drawing;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Terminal;
using Ambystech.Elaris.UI.Widgets;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Rendering;

public class ClippingTests
{
    private static Screen CreateScreen(int width = 20, int height = 6)
        => new(width, height, new VirtualTerminal(width, height));

    private static string Row(Screen screen, int y)
    {
        var chars = new char[screen.Width];
        for (int x = 0; x < screen.Width; x++)
            chars[x] = screen.GetCell(x, y).Character;
        return new string(chars).TrimEnd();
    }

    /// <summary>Draws a run of characters starting at a fixed position, ignoring its own bounds.</summary>
    private sealed class Painter(int x, int y, string text) : Widget
    {
        protected override void OnRender(Screen screen)
            => screen.WriteText(x, y, text, Color.White, Color.Black);
    }

    [Fact]
    public void ClipRectangle_DefaultsToWholeScreen()
    {
        var screen = CreateScreen(20, 6);

        screen.ClipRectangle.Should().Be(new Rectangle(0, 0, 20, 6));
    }

    [Fact]
    public void PushClip_LimitsAllDrawingMethods()
    {
        var screen = CreateScreen(20, 6);

        using (screen.PushClip(new Rectangle(2, 1, 5, 3)))
        {
            screen.WriteText(0, 1, "abcdefghij", Color.White, Color.Black);
            screen.SetCell(10, 2, new Cell('X', Color.White, Color.Black));
            screen.SetCell(3, 2, new Cell('Y', Color.White, Color.Black));
            screen.FillRectangle(new Rectangle(0, 3, 20, 3), '#', Color.White, Color.Black);
            screen.DrawRectangle(new Rectangle(0, 0, 20, 6), Color.White, Color.Black);
        }

        Row(screen, 0).Should().BeEmpty("row 0 is above the clip");
        Row(screen, 1).Should().Be("  cdefg");
        Row(screen, 2).Should().Be("   Y");
        Row(screen, 3).Should().Be("  #####");
        Row(screen, 4).Should().BeEmpty("row 4 is below the clip");
    }

    [Fact]
    public void PushClip_Nests_AsIntersection_AndPopRestores()
    {
        var screen = CreateScreen(20, 6);

        using (screen.PushClip(new Rectangle(0, 0, 10, 6)))
        {
            using (screen.PushClip(new Rectangle(5, 0, 10, 6)))
            {
                screen.ClipRectangle.Should().Be(new Rectangle(5, 0, 5, 6));
            }

            screen.ClipRectangle.Should().Be(new Rectangle(0, 0, 10, 6));
        }

        screen.ClipRectangle.Should().Be(new Rectangle(0, 0, 20, 6));
    }

    [Fact]
    public void PushOverlay_IgnoresParentClips()
    {
        var screen = CreateScreen(20, 6);

        using (screen.PushClip(new Rectangle(0, 0, 3, 1)))
        using (screen.PushOverlay())
        {
            screen.ClipRectangle.Should().Be(new Rectangle(0, 0, 20, 6));
            screen.WriteText(0, 4, "overlay", Color.White, Color.Black);
        }

        Row(screen, 4).Should().Be("overlay");
    }

    [Fact]
    public void PopClip_WithoutPush_Throws()
    {
        var screen = CreateScreen();

        var act = () => screen.PopClip();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resize_ResetsClip()
    {
        var screen = CreateScreen(20, 6);
        screen.PushClip(new Rectangle(0, 0, 2, 2));

        screen.Resize(30, 8);

        screen.ClipRectangle.Should().Be(new Rectangle(0, 0, 30, 8));
    }

    [Fact]
    public void Container_ClipsChildrenToItsBounds()
    {
        var screen = CreateScreen(20, 6);
        var container = new Container { Bounds = new Rectangle(2, 1, 6, 2) };
        container.Add(new Painter(0, 1, "0123456789"));
        container.Add(new Painter(3, 4, "below"));

        container.Render(screen);

        Row(screen, 1).Should().Be("  234567");
        Row(screen, 4).Should().BeEmpty();
    }

    [Fact]
    public void Container_WithoutSize_DoesNotClip()
    {
        var screen = CreateScreen(20, 6);
        var container = new Container();
        container.Add(new Painter(0, 1, "0123456789"));

        container.Render(screen);

        Row(screen, 1).Should().Be("0123456789");
    }

    [Fact]
    public void Container_ClipChildrenFalse_DoesNotClip()
    {
        var screen = CreateScreen(20, 6);
        var container = new Container { Bounds = new Rectangle(2, 1, 3, 1), ClipChildren = false };
        container.Add(new Painter(0, 1, "0123456789"));

        container.Render(screen);

        Row(screen, 1).Should().Be("0123456789");
    }

    [Fact]
    public void Widget_OwnDrawing_IsNotClippedToItsOwnBounds()
    {
        var screen = CreateScreen(20, 6);
        var root = new Container { Bounds = new Rectangle(0, 0, 20, 6) };
        // Zero-size widget that still draws, like a widget whose size was never set.
        root.Add(new Painter(1, 1, "still drawn"));

        root.Render(screen);

        Row(screen, 1).Should().Be(" still drawn");
    }

    [Fact]
    public void NestedFrames_ClipToInnermostContainer()
    {
        var screen = CreateScreen(30, 8);
        var outer = new Frame("Outer") { Bounds = new Rectangle(0, 0, 30, 8) };
        var inner = new Frame("In") { Bounds = new Rectangle(2, 2, 10, 4) };
        var label = new Label("this text is far too long for the frame")
        {
            Bounds = new Rectangle(3, 3, 40, 1),
        };
        inner.Add(label);
        outer.Add(inner);

        outer.Render(screen);

        // The inner frame spans x = 2..11; its border is at x = 2 and x = 11.
        Row(screen, 3)[3..11].Should().Be("this tex", "the label is cut off inside the inner frame's border");
        screen.GetCell(11, 3).Character.Should().Be('│', "the inner frame's border is not overwritten");
        screen.GetCell(29, 3).Character.Should().Be('│', "the outer frame's border is not overwritten");
    }

    [Fact]
    public void Frame_WithoutBorder_ClipsToFullBounds()
    {
        var screen = CreateScreen(20, 6);
        var frame = new Frame { Bounds = new Rectangle(0, 0, 5, 2), BorderStyle = Enums.BorderStyle.None };
        frame.Add(new Painter(0, 0, "0123456789"));

        frame.Render(screen);

        Row(screen, 0).Should().Be("01234");
    }

    [Fact]
    public void AutoCompletePopup_ExtendsOutsideItsContainer()
    {
        var screen = CreateScreen(30, 8);
        var root = new Container { Bounds = new Rectangle(0, 0, 30, 8) };
        var row = new Container { Bounds = new Rectangle(0, 0, 30, 1) };
        var field = new AutoCompleteTextField { Bounds = new Rectangle(0, 0, 20, 1) };
        row.Add(field);
        root.Add(row);

        field.ShowSuggestions(["apple", "apricot"]);
        root.Render(screen);

        Row(screen, 1).Should().Contain("apple");
        Row(screen, 2).Should().Contain("apricot");
    }

    [Fact]
    public void Render_LeavesClipStackBalanced_WhenChildThrows()
    {
        var screen = CreateScreen(20, 6);
        var container = new Container { Bounds = new Rectangle(1, 1, 5, 2) };
        container.Add(new ThrowingWidget());

        var act = () => container.Render(screen);

        act.Should().Throw<InvalidOperationException>();
        screen.ClipRectangle.Should().Be(new Rectangle(0, 0, 20, 6));
    }

    private sealed class ThrowingWidget : Widget
    {
        protected override void OnRender(Screen screen) => throw new InvalidOperationException();
    }
}
