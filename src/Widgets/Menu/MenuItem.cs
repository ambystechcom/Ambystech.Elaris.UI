namespace Ambystech.Elaris.UI.Widgets.Menu;

/// <summary>
/// Represents a menu item with text, hotkey, and action.
/// </summary>
public class MenuItem()
{
    /// <summary>
    /// Gets or sets the menu item text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hotkey character (will be underlined in display).
    /// </summary>
    public char? Hotkey { get; set; }

    /// <summary>
    /// Gets or sets whether this is a separator item.
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>
    /// Gets or sets whether this item is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this item is checked (for toggle items).
    /// </summary>
    public bool Checked { get; set; }

    /// <summary>
    /// Event raised when the menu item is selected.
    /// </summary>
    public event Action? Selected;

    /// <summary>
    /// Gets or sets the submenu items.
    /// </summary>
    public List<MenuItem> SubItems { get; set; } = new();

    /// <summary>
    /// Gets whether this item has a submenu.
    /// </summary>
    public bool HasSubmenu => SubItems.Count > 0;


    public MenuItem(string text, Action? onSelected = null, char? hotkey = null) : this()
    {
        Text = text;
        Hotkey = hotkey;
        if (onSelected != null)
            Selected += onSelected;
    }

    /// <summary>
    /// Creates a separator menu item.
    /// </summary>
    public static MenuItem Separator()
    {
        return new MenuItem { IsSeparator = true };
    }

    /// <summary>
    /// Invokes the selected action.
    /// </summary>
    public void Invoke()
    {
        if (Enabled && !IsSeparator)
        {
            Selected?.Invoke();
        }
    }
}
