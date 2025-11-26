namespace Ambystech.Elaris.UI.Widgets.Data;

public class TableRow
{
    private readonly Dictionary<TableColumn, TableCell> _cells = new();

    public object? Data { get; set; }
    public bool IsSelected { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Index { get; internal set; }

    public TableRow(object? data = null) => Data = data;

    public TableCell GetCell(TableColumn column)
    {
        if (!_cells.TryGetValue(column, out var cell))
        {
            object? value = column.GetValue(Data);
            cell = column.CreateCell(this, value);
            _cells[column] = cell;
        }
        return cell;
    }

    public void InvalidateCells() => _cells.Clear();
}
