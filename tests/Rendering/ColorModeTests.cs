using System.Drawing;
using System.Text.RegularExpressions;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Terminal;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Layout;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Rendering;

public class ColorModeTests
{
    // ---- Conversion ----

    [Theory]
    [InlineData(0, 0, 0, 16)]          // cube black
    [InlineData(255, 255, 255, 231)]   // cube white
    [InlineData(255, 0, 0, 196)]       // pure red
    [InlineData(0, 255, 0, 46)]        // pure green
    [InlineData(0, 0, 255, 21)]        // pure blue
    [InlineData(95, 135, 175, 67)]     // exact cube color (1,2,3)
    [InlineData(128, 128, 128, 244)]   // mid gray → gray ramp (128)
    [InlineData(70, 70, 70, 238)]      // Theme.Dark Surface → gray ramp (68)
    public void ToPalette256_PicksNearestEntry(int r, int g, int b, int expected)
    {
        ColorConversion.ToPalette256(Color.FromArgb(r, g, b)).Should().Be(expected);
    }

    [Fact]
    public void Palette256_RoundTripsEveryEntryFrom16()
    {
        for (int i = 16; i < 256; i++)
            ColorConversion.ToPalette256(ColorConversion.FromPalette256(i)).Should().Be(i, $"entry {i}");
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(255, 255, 255, 15)]
    [InlineData(200, 0, 0, 1)]
    [InlineData(255, 40, 40, 9)]
    [InlineData(0, 0, 230, 4)]
    [InlineData(128, 128, 128, 8)]
    [InlineData(250, 250, 250, 15)]
    [InlineData(100, 150, 200, 8)]     // Theme.Dark Accent: nearest is bright black (gray)
    public void ToAnsi16_PicksNearestEntry(int r, int g, int b, int expected)
    {
        ColorConversion.ToAnsi16(Color.FromArgb(r, g, b)).Should().Be(expected);
    }

    [Fact]
    public void Luminance_IsZeroForBlackAndOneForWhite()
    {
        ColorConversion.Luminance(Color.Black).Should().Be(0);
        ColorConversion.Luminance(Color.White).Should().BeApproximately(1, 1e-9);
    }

    // ---- Detection ----

    public static TheoryData<string, bool, ColorMode> Environments => new()
    {
        { "NO_COLOR=1;COLORTERM=truecolor;TERM=xterm-256color", false, ColorMode.NoColor },
        { "COLORTERM=truecolor;TERM=xterm-256color", false, ColorMode.TrueColor },
        { "COLORTERM=24bit", false, ColorMode.TrueColor },
        { "WT_SESSION=abc;TERM=xterm", false, ColorMode.TrueColor },
        { "TERM_PROGRAM=iTerm.app;TERM=xterm-256color", false, ColorMode.TrueColor },
        { "TERM_PROGRAM=Apple_Terminal;TERM=xterm-256color", false, ColorMode.Palette256 },
        { "TERM=xterm-256color", false, ColorMode.Palette256 },
        { "TERM=screen-256color", false, ColorMode.Palette256 },
        { "TERM=xterm-direct", false, ColorMode.TrueColor },
        { "TERM=xterm", false, ColorMode.Palette16 },
        { "TERM=linux", false, ColorMode.Palette16 },
        { "TERM=dumb", false, ColorMode.NoColor },
        { "NO_COLOR=", false, ColorMode.Palette16 },       // empty NO_COLOR is ignored
        { "", false, ColorMode.Palette16 },
        { "", true, ColorMode.TrueColor },                  // Windows console
    };

    [Theory]
    [MemberData(nameof(Environments))]
    public void DetectColorMode_UsesEnvironment(string environment, bool isWindows, ColorMode expected)
    {
        var values = environment.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(p => p[0], p => p[1]);

        ColorConversion.DetectColorMode(name => values.GetValueOrDefault(name), isWindows).Should().Be(expected);
    }

    // ---- Rendering ----

    private static (Screen, VirtualTerminal) Create(ColorMode mode)
    {
        var terminal = new VirtualTerminal(20, 3) { ColorMode = mode };
        var screen = new Screen(20, 3, terminal);
        screen.Initialize();
        terminal.TakeOutput();
        return (screen, terminal);
    }

    [Fact]
    public void Screen_UsesTerminalColorMode()
    {
        var (screen, _) = Create(ColorMode.Palette256);

        screen.ColorMode.Should().Be(ColorMode.Palette256);
    }

    [Fact]
    public void Palette256_WritesPaletteCodes()
    {
        var (screen, terminal) = Create(ColorMode.Palette256);

        screen.WriteText(0, 0, "hi", Color.FromArgb(255, 0, 0), Color.FromArgb(0, 0, 255));
        screen.Render();
        var output = terminal.TakeOutput();

        output.Should().Contain("38;5;196").And.Contain("48;5;21").And.NotContain("38;2;").And.NotContain("48;2;");
        terminal.GetCell(0, 0).Foreground.ToArgb().Should().Be(Color.FromArgb(255, 0, 0).ToArgb());
    }

    [Fact]
    public void Palette16_WritesBasicCodes()
    {
        var (screen, terminal) = Create(ColorMode.Palette16);

        screen.WriteText(0, 0, "hi", Color.FromArgb(255, 255, 255), Color.FromArgb(200, 0, 0));
        screen.Render();
        var output = terminal.TakeOutput();

        Regex.IsMatch(output, @"\x1b\[0;97;41m").Should().BeTrue(output);
        output.Should().NotContain("38;").And.NotContain("48;");
    }

    [Fact]
    public void Palette16_KeepsTextReadable_WhenConversionLosesContrast()
    {
        var (screen, terminal) = Create(ColorMode.Palette16);
        var fg = Color.FromArgb(100, 100, 100);
        var bg = Color.FromArgb(160, 160, 160);
        ColorConversion.ContrastRatio(fg, bg).Should().BeGreaterThan(2, "readable in 24-bit color");
        ColorConversion.ToAnsi16(fg).Should().Be(ColorConversion.ToAnsi16(bg), "both become the same gray");

        screen.WriteText(0, 0, "x", fg, bg);
        screen.Render();

        var cell = terminal.GetCell(0, 0);
        ColorConversion.ContrastRatio(cell.Foreground, cell.Background).Should().BeGreaterThan(3);
    }

    [Fact]
    public void Palette16_LeavesLowContrastTextAsAuthored()
    {
        var (screen, terminal) = Create(ColorMode.Palette16);

        // Already unreadable in 24-bit color: not the conversion's fault, so not changed.
        screen.WriteText(0, 0, "x", Color.FromArgb(60, 60, 60), Color.FromArgb(64, 64, 64));
        screen.Render();

        terminal.GetCell(0, 0).Foreground.ToArgb().Should().Be(ColorConversion.Ansi16[ColorConversion.ToAnsi16(Color.FromArgb(60, 60, 60))].ToArgb());
    }

    [Theory]
    [MemberData(nameof(BuiltInThemes))]
    public void BuiltInThemes_StayReadable_InPaletteModes(string name, Theme theme)
    {
        (Color fg, Color bg)[] pairs =
        [
            (theme.Foreground, theme.Background.A == 0 ? Color.Black : theme.Background),
            (theme.AccentForeground, theme.Accent),
            (theme.SelectionForeground, theme.Selection),
            (theme.SurfaceForeground, theme.Surface),
            (theme.Foreground, theme.SurfaceAlt),
            (theme.Foreground, theme.Track),
        ];

        foreach (var mode in new[] { ColorMode.Palette256, ColorMode.Palette16 })
        {
            var (screen, terminal) = Create(mode);
            for (int i = 0; i < pairs.Length; i++)
                screen.WriteText(i, 0, "x", pairs[i].fg, pairs[i].bg);
            screen.Render();

            for (int i = 0; i < pairs.Length; i++)
            {
                var cell = terminal.GetCell(i, 0);
                ColorConversion.ContrastRatio(cell.Foreground, cell.Background)
                    .Should().BeGreaterThan(2, $"{name}, {mode}, pair {i}");
            }
        }
    }

    public static TheoryData<string, Theme> BuiltInThemes => new()
    {
        { "Dark", Theme.Dark },
        { "Light", Theme.Light },
        { "HighContrast", Theme.HighContrast },
    };

    [Fact]
    public void NoColor_WritesNoColorCodes_ButKeepsAttributes()
    {
        var (screen, terminal) = Create(ColorMode.NoColor);

        screen.WriteText(0, 0, "hi", Color.Red, Color.Transparent, bold: true, underline: true);
        screen.Render();
        var output = terminal.TakeOutput();

        output.Should().NotContain("38;").And.NotContain("48;");
        terminal.GetCell(0, 0).Bold.Should().BeTrue();
        terminal.GetCell(0, 0).Underline.Should().BeTrue();
        terminal.IsReverse(0, 0).Should().BeFalse();
    }

    [Fact]
    public void NoColor_ShowsHighlightedBackgroundsInReverseVideo()
    {
        var (screen, terminal) = Create(ColorMode.NoColor);

        screen.WriteText(0, 0, "selected", Color.White, Theme.Dark.Selection);
        screen.WriteText(0, 1, "plain", Color.White, Color.Black);
        screen.Render();

        terminal.IsReverse(0, 0).Should().BeTrue("the selection background differs from the screen background");
        terminal.IsReverse(0, 1).Should().BeFalse("a black background matches the default dark screen");
    }

    [Fact]
    public void NoColor_ComparesWithBaseBackground_ForLightThemes()
    {
        var (screen, terminal) = Create(ColorMode.NoColor);
        screen.BaseBackground = Theme.Light.Background;

        screen.WriteText(0, 0, "page", Theme.Light.Foreground, Theme.Light.Background);
        screen.WriteText(0, 1, "button", Theme.Light.AccentForeground, Theme.Light.Accent);
        screen.Render();

        terminal.IsReverse(0, 0).Should().BeFalse();
        terminal.IsReverse(0, 1).Should().BeTrue();
    }

    [Fact]
    public void ChangingColorMode_RedrawsEverything()
    {
        var (screen, terminal) = Create(ColorMode.TrueColor);
        screen.WriteText(0, 0, "hi", Color.Red, Color.Blue);
        screen.Render();
        terminal.TakeOutput();

        screen.ColorMode = ColorMode.Palette256;
        screen.Clear();
        screen.WriteText(0, 0, "hi", Color.Red, Color.Blue);
        screen.Render();

        terminal.TakeOutput().Should().Contain("38;5;");
    }

    [Fact]
    public void Application_ColorMode_OverridesTerminal()
    {
        var terminal = new VirtualTerminal(30, 3) { ColorMode = ColorMode.TrueColor };
        var app = new Application(terminal) { ColorMode = ColorMode.Palette16, TargetFps = 200 };
        var root = new Container();
        root.Add(new Label("hello") { Bounds = new Rectangle(0, 0, 10, 1), ForegroundColor = Color.FromArgb(250, 10, 10) });
        ColorMode? used = null;
        app.Post(() =>
        {
            used = app.Screen!.ColorMode;
        });

        var run = Task.Factory.StartNew(() => app.Run(root), TaskCreationOptions.LongRunning);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!terminal.GetText().Contains("hello") && DateTime.UtcNow < deadline)
            Thread.Sleep(5);
        var output = terminal.TakeOutput();
        app.Stop();
        run.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();

        used.Should().Be(ColorMode.Palette16);
        output.Should().Contain("\x1b[0;91m").And.NotContain("38;2;");
    }
}
