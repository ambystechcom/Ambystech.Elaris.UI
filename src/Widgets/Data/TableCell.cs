using System.Drawing;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Data;

public class TableCell
{
    public TableColumn Column { get; }
    public TableRow Row { get; }
    public object? Value { get; set; }
    public Widget? Widget { get; protected set; }

    public TableCell(TableColumn column, TableRow row, object? value)
    {
        Column = column;
        Row = row;
        Value = value;
    }

    public virtual void UpdateBounds(int x, int y, int width, int height)
    {
        if (Widget != null)
        {
            Widget.X = x;
            Widget.Y = y;
            Widget.Width = width;
            Widget.Height = height;
        }
    }

    public virtual void Render(Screen screen, int x, int y, int width, bool isSelected,
        Color foreground, Color background)
    {
        if (Widget != null)
        {
            UpdateBounds(x, y, width, 1);
            Widget.Render(screen);
        }
        else
        {
            string text = FormatValue(Value);
            text = TruncateAndAlign(text, width, Column.Alignment);
            screen.WriteText(x, y, text, foreground, background);
        }
    }

    protected virtual string FormatValue(object? value) => value?.ToString() ?? "";

    protected string TruncateAndAlign(string text, int width, HorizontalAlignment alignment)
    {
        if (width <= 0) return string.Empty;

        if (text.Length > width)
            text = width > 1 ? text[..(width - 1)] + "…" : text[..width];

        return alignment switch
        {
            HorizontalAlignment.Center => text.PadLeft((width + text.Length) / 2).PadRight(width),
            HorizontalAlignment.Right => text.PadLeft(width),
            _ => text.PadRight(width)
        };
    }
}
