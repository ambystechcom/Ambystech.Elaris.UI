using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Menu;

/// <summary>
/// A horizontal menu bar with dropdown menus.
/// </summary>
public class MenuBar : Widget
{
    private List<MenuItem> _items = [];
    private int _selectedIndex = -1;
    private int _activeMenuIndex = -1;
    private int _activeSubItemIndex = 0;
    private bool _menuOpen = false;
    private MenuDropdown? _activeDropdown;

    /// <summary>
    /// Gets the menu items.
    /// </summary>
    public IReadOnlyList<MenuItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Event raised when a menu item is selected.
    /// </summary>
    public event Action<MenuItem>? ItemSelected;

    /// <summary>
    /// Gets or sets the background color for selected menu items.
    /// </summary>
    public Color SelectedBackgroundColor { get; set; } = ColorHelper.FromRgb(100, 100, 100);

    /// <summary>
    /// Gets or sets the foreground color for selected menu items.
    /// </summary>
    public Color SelectedForegroundColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the background color for dropdown menus.
    /// </summary>
    public Color DropdownBackgroundColor { get; set; } = ColorHelper.FromRgb(40, 40, 40);

    /// <summary>
    /// Gets or sets the foreground color for dropdown menu items.
    /// </summary>
    public Color DropdownForegroundColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the background color for selected dropdown items.
    /// </summary>
    public Color DropdownSelectedBackgroundColor { get; set; } = ColorHelper.FromRgb(80, 80, 150);

    /// <summary>
    /// Gets or sets the foreground color for disabled dropdown items.
    /// </summary>
    public Color DropdownDisabledForegroundColor { get; set; } = Color.Gray;

    /// <summary>
    /// Gets or sets the border color for dropdown menus.
    /// </summary>
    public Color DropdownBorderColor { get; set; } = Color.Gray;

    /// <summary>
    /// Gets whether this widget can receive keyboard focus.
    /// </summary>
    public override bool IsFocusable => true;

    /// <summary>
    /// Adds a menu item to the menu bar.
    /// </summary>
    public void AddItem(MenuItem item)
    {
        _items.Add(item);
    }

    /// <summary>
    /// Adds a menu item with text and submenu.
    /// </summary>
    public void AddMenu(string text, params MenuItem[] subItems)
    {
        var menu = new MenuItem(text);
        foreach (var subItem in subItems)
        {
            menu.SubItems.Add(subItem);
        }
        _items.Add(menu);
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);

        int currentX = X + 1;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var isSelected = i == _selectedIndex || i == _activeMenuIndex;

            var itemBg = isSelected ? SelectedBackgroundColor : BackgroundColor;
            var itemFg = isSelected ? SelectedForegroundColor : ForegroundColor;

            string displayText = $" {item.Text} ";
            screen.WriteText(currentX, Y, displayText, itemFg, itemBg, bold: isSelected);

            currentX += displayText.Length + 1;
        }
    }

    private void ShowDropdown(int menuIndex)
    {
        HideDropdown();

        if (menuIndex < 0 || menuIndex >= _items.Count)
            return;

        var menu = _items[menuIndex];
        if (!menu.HasSubmenu)
            return;

        int menuX = X + 1;
        for (int i = 0; i < menuIndex; i++)
        {
            menuX += _items[i].Text.Length + 3;
        }

        int menuY = Y + 1;
        int menuWidth = 0;

        foreach (var item in menu.SubItems)
        {
            int itemWidth = item.IsSeparator ? 10 : item.Text.Length + 4;
            if (itemWidth > menuWidth)
                menuWidth = itemWidth;
        }

        menuWidth = Math.Max(menuWidth, 15);
        int menuHeight = menu.SubItems.Count;

        _activeDropdown = new MenuDropdown
        {
            Menu = menu,
            ActiveSubItemIndex = _activeSubItemIndex,
            X = menuX,
            Y = menuY,
            Width = menuWidth,
            Height = menuHeight,
            BackgroundColor = DropdownBackgroundColor,
            ForegroundColor = DropdownForegroundColor,
            SelectedBackgroundColor = DropdownSelectedBackgroundColor,
            DisabledForegroundColor = DropdownDisabledForegroundColor,
            BorderColor = DropdownBorderColor
        };

        var root = GetRootWidget();
        if (root != null)
        {
            root.Add(_activeDropdown);
            root.SortChildrenByZIndex();
        }
    }

    private void HideDropdown()
    {
        if (_activeDropdown != null)
        {
            var root = GetRootWidget();
            if (root != null)
            {
                root.Remove(_activeDropdown);
            }
            _activeDropdown = null;
        }
    }

    private Widget? GetRootWidget()
    {
        var current = (Widget)this;
        while (current.Parent != null)
        {
            current = current.Parent;
        }
        return current;
    }

    private void UpdateDropdown()
    {
        if (_activeDropdown != null)
        {
            _activeDropdown.ActiveSubItemIndex = _activeSubItemIndex;
        }
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        if (!_menuOpen)
        {
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (_selectedIndex > 0)
                        _selectedIndex--;
                    else
                        _selectedIndex = _items.Count - 1;
                    return true;

                case ConsoleKey.RightArrow:
                    if (_selectedIndex < _items.Count - 1)
                        _selectedIndex++;
                    else
                        _selectedIndex = 0;
                    return true;

                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                case ConsoleKey.DownArrow:
                    if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                    {
                        var item = _items[_selectedIndex];
                        if (item.HasSubmenu)
                        {
                            _menuOpen = true;
                            _activeMenuIndex = _selectedIndex;
                            _activeSubItemIndex = 0;
                            ShowDropdown(_activeMenuIndex);
                        }
                        else
                        {
                            item.Invoke();
                            ItemSelected?.Invoke(item);
                        }
                    }
                    return true;
            }
        }
        else
        {
            var activeMenu = _items[_activeMenuIndex];

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    do
                    {
                        _activeSubItemIndex--;
                        if (_activeSubItemIndex < 0)
                            _activeSubItemIndex = activeMenu.SubItems.Count - 1;
                    } while (activeMenu.SubItems[_activeSubItemIndex].IsSeparator);
                    UpdateDropdown();
                    return true;

                case ConsoleKey.DownArrow:
                    do
                    {
                        _activeSubItemIndex++;
                        if (_activeSubItemIndex >= activeMenu.SubItems.Count)
                            _activeSubItemIndex = 0;
                    } while (activeMenu.SubItems[_activeSubItemIndex].IsSeparator);
                    UpdateDropdown();
                    return true;

                case ConsoleKey.LeftArrow:
                    if (_selectedIndex > 0)
                        _selectedIndex--;
                    else
                        _selectedIndex = _items.Count - 1;

                    _activeMenuIndex = _selectedIndex;
                    _activeSubItemIndex = 0;
                    ShowDropdown(_activeMenuIndex);
                    return true;

                case ConsoleKey.RightArrow:
                    if (_selectedIndex < _items.Count - 1)
                        _selectedIndex++;
                    else
                        _selectedIndex = 0;

                    _activeMenuIndex = _selectedIndex;
                    _activeSubItemIndex = 0;
                    ShowDropdown(_activeMenuIndex);
                    return true;

                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    var selectedItem = activeMenu.SubItems[_activeSubItemIndex];
                    if (selectedItem.Enabled && !selectedItem.IsSeparator)
                    {
                        selectedItem.Invoke();
                        ItemSelected?.Invoke(selectedItem);
                        _menuOpen = false;
                        _activeMenuIndex = -1;
                        HideDropdown();
                    }
                    return true;

                case ConsoleKey.Escape:
                    _menuOpen = false;
                    _activeMenuIndex = -1;
                    HideDropdown();
                    return true;
            }
        }

        return base.OnKeyPress(key);
    }

    protected internal override void OnFocus()
    {
        base.OnFocus();
        if (_selectedIndex < 0 && _items.Count > 0)
            _selectedIndex = 0;
    }

    protected internal override void OnBlur()
    {
        base.OnBlur();
        _menuOpen = false;
        _activeMenuIndex = -1;
        HideDropdown();
    }
}
