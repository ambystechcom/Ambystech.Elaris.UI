using System.Collections;
using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Data;

public class Table : Widget
{
    private readonly List<TableColumn> _columns = [];
    private readonly List<TableRow> _rows = [];
    private readonly Dictionary<TableColumn, TableHeaderCell> _headerCells = new();
    private IEnumerable? _dataSource;
    private int _selectedRowIndex = -1;
    private int _verticalScrollOffset = 0;

    public bool ShowHeader { get; set; } = true;
    public bool ShowGridLines { get; set; } = true;

    public string EmptyStateMessage { get; set; } = "No data to display";
    public Widget? EmptyStateWidget { get; set; }

    public Color HeaderForegroundColor { get; set; } = Color.White;
    public Color HeaderBackgroundColor { get; set; } = ColorHelper.FromRgb(60, 60, 60);
    public Color SelectedRowBackgroundColor { get; set; } = ColorHelper.FromRgb(0, 120, 212);
    public Color SelectedRowForegroundColor { get; set; } = Color.White;
    public Color GridLineColor { get; set; } = ColorHelper.FromRgb(80, 80, 80);
    public Color EmptyStateForegroundColor { get; set; } = Color.Gray;

    public IEnumerable? DataSource
    {
        get => _dataSource;
        set
        {
            _dataSource = value;
            RebuildRows();
        }
    }

    public IReadOnlyList<TableColumn> Columns => _columns.AsReadOnly();
    public IReadOnlyList<TableRow> Rows => _rows.AsReadOnly();

    public int SelectedRowIndex
    {
        get => _selectedRowIndex;
        set
        {
            if (value < -1) value = -1;
            if (value >= _rows.Count) value = _rows.Count - 1;

            if (_selectedRowIndex != value)
            {
                _selectedRowIndex = value;
                if (_selectedRowIndex >= 0)
                {
                    EnsureVisible(_selectedRowIndex);
                    RowSelected?.Invoke(_rows[_selectedRowIndex]);
                }
            }
        }
    }

    public TableRow? SelectedRow => _selectedRowIndex >= 0 && _selectedRowIndex < _rows.Count
        ? _rows[_selectedRowIndex] : null;

    public event Action<TableRow>? RowSelected;
    public event Action<TableRow>? RowActivated;

    public override bool IsFocusable => true;

    public void AddColumn(TableColumn column)
    {
        _columns.Add(column);
        _headerCells[column] = column.CreateHeaderCell();
        InvalidateAllRows();
    }

    public TableColumn AddColumn(string name, int width = 15)
    {
        var column = new TableColumn(name, width);
        AddColumn(column);
        return column;
    }

    public TableRow AddRow(object? data = null)
    {
        var row = new TableRow(data) { Index = _rows.Count };
        _rows.Add(row);
        return row;
    }

    public void RemoveRow(TableRow row)
    {
        int index = _rows.IndexOf(row);
        if (index < 0) return;

        _rows.RemoveAt(index);
        ReindexRows();

        if (_selectedRowIndex >= _rows.Count)
            _selectedRowIndex = _rows.Count - 1;
    }

    public void ClearRows()
    {
        _rows.Clear();
        _selectedRowIndex = -1;
        _verticalScrollOffset = 0;
    }

    public void Refresh()
    {
        if (_dataSource != null)
            RebuildRows();
        else
            InvalidateAllRows();
    }

    private void RebuildRows()
    {
        ClearRows();
        if (_dataSource == null) return;

        foreach (var item in _dataSource)
        {
            AddRow(item);
        }

        if (_rows.Count > 0)
            _selectedRowIndex = 0;
    }

    private void InvalidateAllRows()
    {
        foreach (var row in _rows)
            row.InvalidateCells();
    }

    private void ReindexRows()
    {
        for (int i = 0; i < _rows.Count; i++)
            _rows[i].Index = i;
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0) return;

        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);

        if (_rows.Count == 0)
        {
            RenderEmptyState(screen);
            return;
        }

        int headerHeight = ShowHeader ? 2 : 0;
        int visibleRows = Height - headerHeight;

        if (ShowHeader)
        {
            RenderHeader(screen, Y);
            RenderHorizontalLine(screen, Y + 1);
        }

        int startRow = _verticalScrollOffset;
        int endRow = Math.Min(startRow + visibleRows, _rows.Count);
        int currentY = Y + headerHeight;

        for (int i = startRow; i < endRow; i++)
        {
            RenderRow(screen, _rows[i], currentY, i == _selectedRowIndex);
            currentY++;
        }

        if (ShowGridLines)
            RenderVerticalLines(screen, headerHeight);
    }

    protected virtual void RenderEmptyState(Screen screen)
    {
        if (EmptyStateWidget != null)
        {
            EmptyStateWidget.X = X;
            EmptyStateWidget.Y = Y + Height / 2;
            EmptyStateWidget.Width = Width;
            EmptyStateWidget.Height = 1;
            EmptyStateWidget.Render(screen);
        }
        else
        {
            int messageY = Y + Height / 2;
            int messageX = X + Math.Max(0, (Width - EmptyStateMessage.Length) / 2);
            screen.WriteText(messageX, messageY, EmptyStateMessage,
                EmptyStateForegroundColor, BackgroundColor);
        }
    }

    protected virtual void RenderHeader(Screen screen, int y)
    {
        screen.FillRectangle(new Rectangle(X, y, Width, 1), ' ',
            HeaderForegroundColor, HeaderBackgroundColor);

        int currentX = X;
        foreach (var column in _columns)
        {
            if (!column.IsVisible) continue;
            if (currentX >= X + Width) break;

            int cellWidth = Math.Min(column.Width, X + Width - currentX);
            var headerCell = _headerCells[column];
            headerCell.Render(screen, currentX, y, cellWidth, false,
                HeaderForegroundColor, HeaderBackgroundColor);

            currentX += column.Width + 1;
        }
    }

    protected virtual void RenderRow(Screen screen, TableRow row, int y, bool isSelected)
    {
        var fg = isSelected ? SelectedRowForegroundColor : ForegroundColor;
        var bg = isSelected ? SelectedRowBackgroundColor : BackgroundColor;

        screen.FillRectangle(new Rectangle(X, y, Width, 1), ' ', fg, bg);

        int currentX = X;
        foreach (var column in _columns)
        {
            if (!column.IsVisible) continue;
            if (currentX >= X + Width) break;

            int cellWidth = Math.Min(column.Width, X + Width - currentX);
            var cell = row.GetCell(column);
            cell.Value = column.GetValue(row.Data);
            cell.Render(screen, currentX, y, cellWidth, isSelected, fg, bg);

            currentX += column.Width + 1;
        }
    }

    protected virtual void RenderHorizontalLine(Screen screen, int y)
    {
        for (int x = X; x < X + Width; x++)
        {
            screen.SetCell(x, y, new Cell('─', GridLineColor, BackgroundColor));
        }
    }

    protected virtual void RenderVerticalLines(Screen screen, int startYOffset)
    {
        int currentX = X;
        foreach (var column in _columns)
        {
            if (!column.IsVisible) continue;
            currentX += column.Width;
            if (currentX >= X + Width - 1) break;

            for (int y = Y + startYOffset; y < Y + Height; y++)
            {
                screen.SetCell(currentX, y, new Cell('│', GridLineColor, BackgroundColor));
            }
            currentX++;
        }
    }

    private int GetVisibleRowCount()
    {
        int headerHeight = ShowHeader ? 2 : 0;
        return Math.Max(1, Height - headerHeight);
    }

    private void EnsureVisible(int rowIndex)
    {
        int visibleRows = GetVisibleRowCount();

        if (rowIndex < _verticalScrollOffset)
            _verticalScrollOffset = rowIndex;
        else if (rowIndex >= _verticalScrollOffset + visibleRows)
            _verticalScrollOffset = rowIndex - visibleRows + 1;
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (_selectedRowIndex > 0)
                    SelectedRowIndex--;
                return true;

            case ConsoleKey.DownArrow:
                if (_selectedRowIndex < _rows.Count - 1)
                    SelectedRowIndex++;
                return true;

            case ConsoleKey.Home:
                if (_rows.Count > 0)
                    SelectedRowIndex = 0;
                return true;

            case ConsoleKey.End:
                if (_rows.Count > 0)
                    SelectedRowIndex = _rows.Count - 1;
                return true;

            case ConsoleKey.PageUp:
                int pageSize = GetVisibleRowCount();
                SelectedRowIndex = Math.Max(0, _selectedRowIndex - pageSize);
                return true;

            case ConsoleKey.PageDown:
                pageSize = GetVisibleRowCount();
                SelectedRowIndex = Math.Min(_rows.Count - 1, _selectedRowIndex + pageSize);
                return true;

            case ConsoleKey.Enter:
                if (_selectedRowIndex >= 0 && _selectedRowIndex < _rows.Count)
                    RowActivated?.Invoke(_rows[_selectedRowIndex]);
                return true;
        }

        return base.OnKeyPress(key);
    }

    protected internal override void OnFocus()
    {
        base.OnFocus();
        if (_selectedRowIndex < 0 && _rows.Count > 0)
            _selectedRowIndex = 0;
    }
}
