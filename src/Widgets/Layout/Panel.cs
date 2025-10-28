using System.Drawing;
namespace Ambystech.Elaris.UI.Widgets.Layout;

/// <summary>
/// A collapsible panel widget with a header.
/// </summary>
public class Panel : Frame
{
    private bool _collapsed = false;
    private int _expandedHeight;
    private string _header = string.Empty;

    /// <summary>
    /// Gets or sets whether the panel is collapsed.
    /// </summary>
    public bool Collapsed
    {
        get => _collapsed;
        set
        {
            if (_collapsed != value)
            {
                _collapsed = value;
                OnCollapsedChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the panel header text.
    /// </summary>
    public string Header
    {
        get => _header;
        set
        {
            _header = value ?? string.Empty;
            Title = FormatHeader();
        }
    }

    public Panel() : base()
    {
        _expandedHeight = Height;
    }

    public Panel(string header) : base()
    {
        _header = header ?? string.Empty;
        _expandedHeight = Height;
        Title = FormatHeader();
    }

    private string FormatHeader()
    {
        string indicator = _collapsed ? "▶" : "▼";
        return string.IsNullOrEmpty(_header) ? indicator : $"{indicator} {_header}";
    }

    private void OnCollapsedChanged()
    {
        if (_collapsed)
        {
            _expandedHeight = Height;
            Height = 3;

            foreach (var child in Children)
            {
                child.Visible = false;
            }
        }
        else
        {
            Height = _expandedHeight;

            foreach (var child in Children)
            {
                child.Visible = true;
            }
        }

        Title = FormatHeader();
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.Enter)
        {
            Collapsed = !Collapsed;
            return true;
        }

        return base.OnKeyPress(key);
    }

    protected internal override bool OnMouseClick(Point position)
    {
        if (position.Y == Y && position.X >= X && position.X < X + Width)
        {
            Collapsed = !Collapsed;
            return true;
        }

        return base.OnMouseClick(position);
    }

    /// <summary>
    /// Toggles the collapsed state.
    /// </summary>
    public void Toggle()
    {
        Collapsed = !Collapsed;
    }

    /// <summary>
    /// Expands the panel.
    /// </summary>
    public void Expand()
    {
        Collapsed = false;
    }

    /// <summary>
    /// Collapses the panel.
    /// </summary>
    public void Collapse()
    {
        Collapsed = true;
    }
}
