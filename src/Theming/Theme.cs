using System.Drawing;
using Ambystech.Elaris.UI.Core;

namespace Ambystech.Elaris.UI;

/// <summary>
/// A set of colors that widgets use when their own color properties are not set.
/// </summary>
/// <remarks>
/// <para>
/// Set <see cref="Application.Theme"/> to theme a whole application, or
/// <see cref="Widgets.Widget.Theme"/> to theme one widget and its children. Widgets use
/// the nearest theme up their parent chain, or <see cref="Dark"/> if there is none.
/// Colors set directly on a widget (for example <c>button.BackgroundColor = ...</c>)
/// always take precedence over the theme.
/// </para>
/// <para>
/// Themes are immutable records. Derive a custom theme with <c>with</c>:
/// <code>var theme = Theme.Dark with { Accent = Color.Orange, Selection = Color.DarkOrange };</code>
/// </para>
/// </remarks>
public sealed record Theme
{
    /// <summary>Name of the theme, for display.</summary>
    public string Name { get; init; } = "Custom";

    /// <summary>Default text color.</summary>
    public Color Foreground { get; init; } = Color.White;

    /// <summary>
    /// Screen background. Transparent uses the terminal's own background; any other color
    /// fills the screen behind all widgets.
    /// </summary>
    public Color Background { get; init; } = Color.Transparent;

    /// <summary>Secondary text: placeholders, line numbers, tree connectors, disabled items, empty states.</summary>
    public Color MutedForeground { get; init; } = Color.Gray;

    /// <summary>Lines: menu and button borders, table grid lines, scrollbar tracks.</summary>
    public Color Border { get; init; } = Color.Gray;

    /// <summary>Background of the focused or active element: focused button, active tab.</summary>
    public Color Accent { get; init; } = ColorHelper.FromRgb(100, 150, 200);

    /// <summary>Text on <see cref="Accent"/>.</summary>
    public Color AccentForeground { get; init; } = Color.White;

    /// <summary>Background of selected items in lists, trees, tables, menus and suggestions.</summary>
    public Color Selection { get; init; } = ColorHelper.FromRgb(50, 100, 150);

    /// <summary>Text on <see cref="Selection"/>.</summary>
    public Color SelectionForeground { get; init; } = Color.White;

    /// <summary>Background of raised elements: buttons, table headers, inactive tabs.</summary>
    public Color Surface { get; init; } = ColorHelper.FromRgb(70, 70, 70);

    /// <summary>Text on <see cref="Surface"/>.</summary>
    public Color SurfaceForeground { get; init; } = Color.White;

    /// <summary>Background of popups and bars: dropdowns, suggestion lists, tab bars, pressed buttons.</summary>
    public Color SurfaceAlt { get; init; } = ColorHelper.FromRgb(40, 40, 40);

    /// <summary>Background of multi-line text editors.</summary>
    public Color InputBackground { get; init; } = Color.Black;

    /// <summary>Background of tracks and bars: unfilled progress, status bar.</summary>
    public Color Track { get; init; } = Color.DarkGray;

    /// <summary>Positive state: filled progress, checkmarks.</summary>
    public Color Success { get; init; } = Color.Green;

    /// <summary>Highlights: checkbox brackets, matching brackets in the code editor.</summary>
    public Color Warning { get; init; } = Color.Yellow;

    /// <summary>
    /// The default theme: light text on the terminal's own background.
    /// </summary>
    public static Theme Dark { get; } = new() { Name = "Dark" };

    /// <summary>
    /// Dark text on a light background. Fills the screen with its background.
    /// </summary>
    public static Theme Light { get; } = new()
    {
        Name = "Light",
        Foreground = ColorHelper.FromRgb(30, 30, 30),
        Background = ColorHelper.FromRgb(250, 250, 250),
        MutedForeground = ColorHelper.FromRgb(110, 110, 110),
        Border = ColorHelper.FromRgb(160, 160, 160),
        Accent = ColorHelper.FromRgb(0, 102, 204),
        AccentForeground = Color.White,
        Selection = ColorHelper.FromRgb(190, 220, 245),
        SelectionForeground = ColorHelper.FromRgb(0, 0, 0),
        Surface = ColorHelper.FromRgb(225, 225, 225),
        SurfaceForeground = ColorHelper.FromRgb(30, 30, 30),
        SurfaceAlt = ColorHelper.FromRgb(238, 238, 238),
        InputBackground = Color.White,
        Track = ColorHelper.FromRgb(205, 205, 205),
        Success = ColorHelper.FromRgb(0, 135, 60),
        Warning = ColorHelper.FromRgb(175, 110, 0),
    };

    /// <summary>
    /// Maximum contrast: white on black, with yellow and cyan highlights.
    /// </summary>
    public static Theme HighContrast { get; } = new()
    {
        Name = "High Contrast",
        Foreground = Color.White,
        Background = Color.Black,
        MutedForeground = ColorHelper.FromRgb(200, 200, 200),
        Border = Color.White,
        Accent = Color.Yellow,
        AccentForeground = Color.Black,
        Selection = Color.Cyan,
        SelectionForeground = Color.Black,
        Surface = Color.Black,
        SurfaceForeground = Color.White,
        SurfaceAlt = Color.Black,
        InputBackground = Color.Black,
        Track = ColorHelper.FromRgb(90, 90, 90),
        Success = Color.Lime,
        Warning = Color.Yellow,
    };
}
