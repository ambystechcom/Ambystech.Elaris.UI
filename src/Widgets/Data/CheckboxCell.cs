using System.Drawing;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Widgets.Input;

namespace Ambystech.Elaris.UI.Widgets.Data;

public class CheckboxCell : TableCell
{
    private readonly Checkbox _checkbox;

    public CheckboxCell(TableColumn column, TableRow row, object? value) : base(column, row, value)
    {
        _checkbox = new Checkbox { Label = string.Empty };
        Widget = _checkbox;
    }

    public Checkbox Checkbox => _checkbox;

    public override void Render(Screen screen, int x, int y, int width, bool isSelected,
        Color foreground, Color background)
    {
        bool isChecked = Value switch
        {
            bool b => b,
            int i => i != 0,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1",
            _ => false
        };

        _checkbox.IsChecked = isChecked;
        _checkbox.ForegroundColor = foreground;
        _checkbox.BackgroundColor = background;
        UpdateBounds(x, y, width, 1);
        _checkbox.Render(screen);
    }
}
