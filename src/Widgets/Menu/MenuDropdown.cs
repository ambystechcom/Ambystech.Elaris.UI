using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Menu;

/// <summary>
/// Internal widget for rendering menu dropdowns with proper Z-index layering.
/// </summary>
internal class MenuDropdown : Widget
{
    public MenuItem Menu { get; set; } = new MenuItem();
    public int ActiveSubItemIndex { get; set; } = 0;

    /// <summary>
    /// Gets or sets the background color for selected dropdown items.
    /// </summary>
    public Color SelectedBackgroundColor { get; set; } = ColorHelper.FromRgb(80, 80, 150);

    /// <summary>
    /// Gets or sets the foreground color for disabled dropdown items.
    /// </summary>
    public Color DisabledForegroundColor { get; set; } = Color.Gray;

    /// <summary>
    /// Gets or sets the border color for the dropdown.
    /// </summary>
    public Color BorderColor { get; set; } = Color.Gray;

    public MenuDropdown()
    {
        ZIndex = 1000; // Ensure dropdown renders on top
    }

    protected override void OnRender(Screen screen)
    {
        if (!Menu.HasSubmenu || Width <= 0 || Height <= 0)
            return;

        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);

        DrawBorder(screen, Bounds, BorderColor, BackgroundColor);

        for (int i = 0; i < Math.Min(Menu.SubItems.Count, Height); i++)
        {
            var subItem = Menu.SubItems[i];
            int itemY = Y + i;

            if (subItem.IsSeparator)
            {
                for (int x = X + 1; x < X + Width - 1; x++)
                {
                    screen.SetCell(x, itemY, new Cell('─', BorderColor, BackgroundColor));
                }
            }
            else
            {
                var isSelected = i == ActiveSubItemIndex;
                var itemBg = isSelected ? SelectedBackgroundColor : BackgroundColor;
                var itemFg = subItem.Enabled ? ForegroundColor : DisabledForegroundColor;

                string prefix = subItem.Checked ? "✓ " : "  ";
                string displayText = prefix + subItem.Text;

                displayText = displayText.PadRight(Width - 2);

                screen.WriteText(X + 1, itemY, displayText, itemFg, itemBg);
            }
        }
    }

    private void DrawBorder(Screen screen, Rectangle bounds, Color fg, Color bg)
    {
        for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
        {
            screen.SetCell(x, bounds.Y, new Cell('─', fg, bg));
            screen.SetCell(x, bounds.Y + bounds.Height - 1, new Cell('─', fg, bg));
        }

        for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
        {
            screen.SetCell(bounds.X, y, new Cell('│', fg, bg));
            screen.SetCell(bounds.X + bounds.Width - 1, y, new Cell('│', fg, bg));
        }

        screen.SetCell(bounds.X, bounds.Y, new Cell('┌', fg, bg));
        screen.SetCell(bounds.X + bounds.Width - 1, bounds.Y, new Cell('┐', fg, bg));
        screen.SetCell(bounds.X, bounds.Y + bounds.Height - 1, new Cell('└', fg, bg));
        screen.SetCell(bounds.X + bounds.Width - 1, bounds.Y + bounds.Height - 1, new Cell('┘', fg, bg));
    }
}
