using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Input;

/// <summary>
/// A list box widget with selectable items and scrolling support.
/// </summary>
public class ListBox : Widget
{
    private List<string> _items = new();
    private int _selectedIndex = -1;
    private int _scrollOffset = 0;
    private bool _hasFocus = false;

    /// <summary>
    /// Gets the list of items.
    /// </summary>
    public List<string> Items => _items;

    /// <summary>
    /// Gets or sets the selected item index.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int oldIndex = _selectedIndex;
            _selectedIndex = Math.Max(-1, Math.Min(value, _items.Count - 1));

            if (oldIndex != _selectedIndex)
            {
                SelectionChanged?.Invoke(_selectedIndex);
            }

            // Adjust scroll offset to keep selection visible
            if (_selectedIndex >= 0 && Height > 0)
            {
                if (_selectedIndex < _scrollOffset)
                {
                    _scrollOffset = _selectedIndex;
                }
                else if (_selectedIndex >= _scrollOffset + Height)
                {
                    _scrollOffset = _selectedIndex - Height + 1;
                }
            }
        }
    }

    /// <summary>
    /// Gets the selected item text, or null if no selection.
    /// </summary>
    public string? SelectedItem
    {
        get => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;
    }

    /// <summary>
    /// Gets or sets the background color for the selected item.
    /// </summary>
    public Color SelectionBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the foreground color for the selected item.
    /// </summary>
    public Color SelectionForegroundColor { get; set; }

    /// <summary>
    /// Gets or sets the color of the scrollbar track.
    /// </summary>
    public Color ScrollbarTrackColor { get; set; } = Color.Gray;

    /// <summary>
    /// Gets or sets the color of the scrollbar thumb.
    /// </summary>
    public Color ScrollbarThumbColor { get; set; } = Color.White;

    /// <summary>
    /// Event raised when the selected index changes.
    /// </summary>
    public event Action<int>? SelectionChanged;

    /// <summary>
    /// Gets whether this widget can receive keyboard focus.
    /// </summary>
    public override bool IsFocusable => true;

    public ListBox()
    {
        SelectionBackgroundColor = ColorHelper.FromRgb(50, 100, 150);
        SelectionForegroundColor = Color.White;
    }

    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    public void AddItem(string item)
    {
        _items.Add(item ?? string.Empty);
    }

    /// <summary>
    /// Removes an item at the specified index.
    /// </summary>
    public void RemoveItemAt(int index)
    {
        if (index >= 0 && index < _items.Count)
        {
            _items.RemoveAt(index);

            if (_selectedIndex >= _items.Count)
            {
                SelectedIndex = _items.Count - 1;
            }
        }
    }

    /// <summary>
    /// Clears all items.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        _selectedIndex = -1;
        _scrollOffset = 0;
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);

        int visibleCount = Math.Min(Height, _items.Count - _scrollOffset);

        for (int i = 0; i < visibleCount; i++)
        {
            int itemIndex = _scrollOffset + i;
            if (itemIndex >= _items.Count)
                break;

            string item = _items[itemIndex];
            int itemY = Y + i;

            bool isSelected = itemIndex == _selectedIndex;
            Color bgColor = isSelected ? SelectionBackgroundColor : BackgroundColor;
            Color fgColor = isSelected ? SelectionForegroundColor : ForegroundColor;

            int maxLength = Width;
            string displayItem = item.Length > maxLength ? item.Substring(0, maxLength) : item.PadRight(maxLength);

            screen.WriteText(X, itemY, displayItem, fgColor, bgColor, bold: isSelected);
        }

        if (_items.Count > Height && Width > 0)
        {
            int scrollbarX = X + Width - 1;

            int scrollbarHeight = Math.Max(1, Height * Height / _items.Count);
            int scrollbarY = Y + _scrollOffset * Height / _items.Count;

            for (int y = Y; y < Y + Height; y++)
            {
                screen.SetCell(scrollbarX, y, new Cell('│', ScrollbarTrackColor, BackgroundColor));
            }

            for (int y = scrollbarY; y < scrollbarY + scrollbarHeight && y < Y + Height; y++)
            {
                screen.SetCell(scrollbarX, y, new Cell('█', ScrollbarThumbColor, BackgroundColor));
            }
        }
    }

    protected internal override void OnFocus()
    {
        base.OnFocus();
        _hasFocus = true;

        if (_selectedIndex == -1 && _items.Count > 0)
        {
            SelectedIndex = 0;
        }
    }

    protected internal override void OnBlur()
    {
        base.OnBlur();
        _hasFocus = false;
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (_selectedIndex > 0)
                {
                    SelectedIndex--;
                }
                return true;

            case ConsoleKey.DownArrow:
                if (_selectedIndex < _items.Count - 1)
                {
                    SelectedIndex++;
                }
                return true;

            case ConsoleKey.Home:
                if (_items.Count > 0)
                {
                    SelectedIndex = 0;
                }
                return true;

            case ConsoleKey.End:
                if (_items.Count > 0)
                {
                    SelectedIndex = _items.Count - 1;
                }
                return true;

            case ConsoleKey.PageUp:
                if (_items.Count > 0)
                {
                    SelectedIndex = Math.Max(0, _selectedIndex - Height);
                }
                return true;

            case ConsoleKey.PageDown:
                if (_items.Count > 0)
                {
                    SelectedIndex = Math.Min(_items.Count - 1, _selectedIndex + Height);
                }
                return true;
        }

        return base.OnKeyPress(key);
    }

    protected override void OnBoundsChanged()
    {
        base.OnBoundsChanged();

        if (_selectedIndex >= 0 && Height > 0)
        {
            if (_selectedIndex >= _scrollOffset + Height)
            {
                _scrollOffset = _selectedIndex - Height + 1;
            }
        }
    }
}
