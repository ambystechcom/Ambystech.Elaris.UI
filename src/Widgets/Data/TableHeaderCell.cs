using System.Drawing;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Data;

public class TableHeaderCell
{
    public TableColumn Column { get; }
    public Widget? Widget { get; protected set; }

    public TableHeaderCell(TableColumn column) => Column = column;

    public virtual void Render(Screen screen, int x, int y, int width, bool isSelected,
        Color foreground, Color background)
    {
        if (Widget != null)
        {
            Widget.X = x;
            Widget.Y = y;
            Widget.Width = width;
            Widget.Height = 1;
            Widget.Render(screen);
        }
        else
        {
            string text = Column.Name;
            if (text.Length > width)
                text = width > 1 ? text[..(width - 1)] + "…" : text[..width];
            text = text.PadRight(width);
            screen.WriteText(x, y, text, foreground, background, bold: true);
        }
    }
}
