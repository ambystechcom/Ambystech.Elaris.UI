using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets;

/// <summary>
/// Base class for all UI widgets.
/// </summary>
public abstract class Widget
{
    private Rectangle _bounds;
    private bool _visible = true;
    private bool _enabled = true;
    protected  bool _hasFocus = false;
    private Widget? _parent;
    private readonly List<Widget> _children = [];
    private int _zIndex = 0;

    /// <summary>
    /// Gets or sets the bounds of this widget.
    /// </summary>
    public Rectangle Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds != value)
            {
                _bounds = value;
                OnBoundsChanged();
            }
        }
    }

    public int X
    {
        get => _bounds.X;
        set => Bounds = new Rectangle(value, _bounds.Y, _bounds.Width, _bounds.Height);
    }

    public int Y
    {
        get => _bounds.Y;
        set => Bounds = new Rectangle(_bounds.X, value, _bounds.Width, _bounds.Height);
    }

    public int Width
    {
        get => _bounds.Width;
        set => Bounds = new Rectangle(_bounds.X, _bounds.Y, value, _bounds.Height);
    }

    public int Height
    {
        get => _bounds.Height;
        set => Bounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, value);
    }

    public bool HasFocus
    {
        get
        {
            if (IsFocusable)
            {
                return _hasFocus;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets or sets whether this widget is visible.
    /// </summary>
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                OnVisibleChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this widget is enabled.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnEnabledChanged();
            }
        }
    }

    /// <summary>
    /// Gets the parent widget.
    /// </summary>
    public Widget? Parent => _parent;

    /// <summary>
    /// Gets the children of this widget.
    /// </summary>
    public IReadOnlyList<Widget> Children => _children.AsReadOnly();

    /// <summary>
    /// Gets or sets the foreground color.
    /// </summary>
    public Color ForegroundColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.Transparent;

    /// <summary>
    /// Gets or sets the Z-index for rendering order. Higher values render on top.
    /// </summary>
    public int ZIndex
    {
        get => _zIndex;
        set
        {
            if (_zIndex != value)
            {
                _zIndex = value;
                _parent?.SortChildrenByZIndex();
            }
        }
    }

    /// <summary>
    /// Gets whether this widget can receive keyboard focus.
    /// Override in derived classes to indicate the widget is focusable.
    /// </summary>
    public virtual bool IsFocusable => false;

    /// <summary>
    /// Adds a child widget.
    /// </summary>
    public void Add(Widget child)
    {
        if (child._parent != null)
            throw new InvalidOperationException("Widget already has a parent");

        _children.Add(child);
        child._parent = this;
        OnChildAdded(child);
    }

    /// <summary>
    /// Removes a child widget.
    /// </summary>
    public bool Remove(Widget child)
    {
        if (_children.Remove(child))
        {
            child._parent = null;
            OnChildRemoved(child);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes all child widgets.
    /// </summary>
    public void Clear()
    {
        foreach (var child in _children.ToList())
        {
            Remove(child);
        }
    }

    /// <summary>
    /// Sorts children by Z-index for proper rendering order.
    /// </summary>
    internal void SortChildrenByZIndex()
    {
        _children.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
    }

    /// <summary>
    /// Renders this widget and its children to the screen.
    /// </summary>
    public void Render(Screen screen)
    {
        if (!Visible)
            return;

        OnRender(screen);

        foreach (var child in _children)
        {
            child.Render(screen);
        }
    }

    /// <summary>
    /// Called when the widget needs to render itself.
    /// </summary>
    protected abstract void OnRender(Screen screen);

    /// <summary>
    /// Called when the bounds change.
    /// </summary>
    protected virtual void OnBoundsChanged() { }

    /// <summary>
    /// Called when the visible state changes.
    /// </summary>
    protected virtual void OnVisibleChanged() { }

    /// <summary>
    /// Called when the enabled state changes.
    /// </summary>
    protected virtual void OnEnabledChanged() { }

    /// <summary>
    /// Called when a child is added.
    /// </summary>
    protected virtual void OnChildAdded(Widget child) { }

    /// <summary>
    /// Called when a child is removed.
    /// </summary>
    protected virtual void OnChildRemoved(Widget child) { }

    /// <summary>
    /// Called when the widget receives focus.
    /// </summary>
    protected internal virtual void OnFocus() { }

    /// <summary>
    /// Called when the widget loses focus.
    /// </summary>
    protected internal virtual void OnBlur() { }

    /// <summary>
    /// Called when a key is pressed.
    /// </summary>
    protected internal virtual bool OnKeyPress(ConsoleKeyInfo key) => false;

    /// <summary>
    /// Called when the mouse is clicked.
    /// </summary>
    protected internal virtual bool OnMouseClick(Point position) => false;
}
