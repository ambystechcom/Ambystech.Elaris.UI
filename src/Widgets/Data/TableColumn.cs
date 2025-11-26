using Ambystech.Elaris.UI.Enums;

namespace Ambystech.Elaris.UI.Widgets.Data;

public class TableColumn
{
    public string Name { get; set; }
    public string? PropertyName { get; set; }
    public int Width { get; set; }
    public int MinWidth { get; set; } = 4;
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
    public bool IsVisible { get; set; } = true;
    public Func<object?, object?>? ValueGetter { get; set; }

    public TableColumn(string name, int width = 15)
    {
        Name = name;
        PropertyName = name;
        Width = width;
    }

    public TableColumn BindTo(string propertyName)
    {
        PropertyName = propertyName;
        return this;
    }

    public TableColumn BindTo<T>(Func<T, object?> getter)
    {
        ValueGetter = obj => obj is T t ? getter(t) : null;
        return this;
    }

    public object? GetValue(object? data)
    {
        if (data == null) return null;

        if (ValueGetter != null)
            return ValueGetter(data);

        var propName = PropertyName ?? Name;
        var property = data.GetType().GetProperty(propName);
        return property?.GetValue(data);
    }

    public virtual TableHeaderCell CreateHeaderCell() => new TableHeaderCell(this);

    public virtual TableCell CreateCell(TableRow row, object? value) => new TableCell(this, row, value);
}
