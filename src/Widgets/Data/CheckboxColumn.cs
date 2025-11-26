namespace Ambystech.Elaris.UI.Widgets.Data;

public class CheckboxColumn : TableColumn
{
    public CheckboxColumn(string name, int width = 5) : base(name, width) { }

    public override TableCell CreateCell(TableRow row, object? value) => new CheckboxCell(this, row, value);
}
