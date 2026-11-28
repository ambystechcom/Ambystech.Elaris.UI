using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Input;
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
    /// <summary>
    /// Whether this widget currently has focus. Set by <see cref="OnFocus"/> and
    /// <see cref="OnBlur"/>; overrides must call the base implementation.
    /// </summary>
    protected bool _hasFocus = false;
    private Widget? _parent;
    private readonly List<Widget> _children = [];
    private int _zIndex = 0;
    private Color? _foregroundColor;
    private Color? _backgroundColor;
    private Theme? _theme;

    /// <summary>
    /// Set on the root widget by <see cref="Application"/>; called by <see cref="Invalidate"/>.
    /// </summary>
    internal Action? RenderRequested { get; set; }

    /// <summary>
    /// For widgets drawn by another widget without being its child (such as a popup list),
    /// the owner whose theme they use.
    /// </summary>
    internal Widget? LogicalParent { get; set; }

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
                Invalidate();
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

    /// <summary>
    /// Gets whether this widget currently has keyboard focus.
    /// </summary>
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
                Invalidate();
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
                Invalidate();
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
    /// <remarks>Defaults to <see cref="DefaultForegroundColor"/>, which comes from the theme.</remarks>
    public Color ForegroundColor
    {
        get => _foregroundColor ?? DefaultForegroundColor;
        set => SetColor(ref _foregroundColor, value);
    }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    /// <remarks>Defaults to <see cref="DefaultBackgroundColor"/>, which comes from the theme.</remarks>
    public Color BackgroundColor
    {
        get => _backgroundColor ?? DefaultBackgroundColor;
        set => SetColor(ref _backgroundColor, value);
    }

    /// <summary>
    /// The foreground color used when <see cref="ForegroundColor"/> is not set.
    /// Defaults to the theme's <see cref="UI.Theme.Foreground"/>.
    /// </summary>
    protected virtual Color DefaultForegroundColor => EffectiveTheme.Foreground;

    /// <summary>
    /// The background color used when <see cref="BackgroundColor"/> is not set.
    /// Defaults to transparent, so the widget shows what is behind it.
    /// </summary>
    protected virtual Color DefaultBackgroundColor => Color.Transparent;

    /// <summary>
    /// Gets or sets the theme for this widget and its children. When <c>null</c> (the default),
    /// the parent's theme is used. See <see cref="EffectiveTheme"/>.
    /// </summary>
    public Theme? Theme
    {
        get => _theme;
        set
        {
            if (_theme != value)
            {
                _theme = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets the theme in effect: this widget's <see cref="Theme"/>, else the nearest ancestor's,
    /// else <see cref="UI.Theme.Dark"/>.
    /// </summary>
    public Theme EffectiveTheme
    {
        get
        {
            for (var widget = this; widget != null; widget = widget._parent ?? widget.LogicalParent)
            {
                if (widget._theme != null)
                    return widget._theme;
            }

            return UI.Theme.Dark;
        }
    }

    /// <summary>
    /// Sets an optional color field and asks for a redraw if it changed. Use it in the setters
    /// of themed color properties: <c>get => _color ?? EffectiveTheme.Accent; set => SetColor(ref _color, value);</c>
    /// </summary>
    protected void SetColor(ref Color? field, Color value)
    {
        if (field is { } current && current.ToArgb() == value.ToArgb() && current.A == value.A)
            return;

        field = value;
        Invalidate();
    }

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
                Invalidate();
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

        // Keep children ordered by ZIndex; among equal ZIndex, in the order they were added.
        int index = _children.Count;
        while (index > 0 && _children[index - 1].ZIndex > child.ZIndex)
            index--;
        _children.Insert(index, child);
        child._parent = this;
        OnChildAdded(child);
        Invalidate();
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
            Invalidate();
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
    /// Asks the running <see cref="Application"/> to redraw the screen.
    /// </summary>
    /// <remarks>
    /// The application redraws after input, after actions queued with
    /// <see cref="Application.Post"/> or <see cref="Application.InvokeAsync(Action)"/>, after a
    /// resize, and after changes to <see cref="Bounds"/>, <see cref="Visible"/>, <see cref="Enabled"/>,
    /// colors, <see cref="ZIndex"/> or children. Call this when a widget's appearance changes for
    /// any other reason. It can be called from any thread, but widget state should only be changed
    /// on the UI thread.
    /// </remarks>
    public void Invalidate()
    {
        var root = this;
        while (root._parent != null)
            root = root._parent;

        root.RenderRequested?.Invoke();
    }

    /// <summary>
    /// Sorts children by Z-index for proper rendering order.
    /// </summary>
    internal void SortChildrenByZIndex()
    {
        // Stable: children with equal ZIndex keep their relative order.
        var ordered = _children.OrderBy(c => c.ZIndex).ToList();
        _children.Clear();
        _children.AddRange(ordered);
    }

    /// <summary>
    /// Renders this widget and its children to the screen.
    /// </summary>
    public void Render(Screen screen)
    {
        if (!Visible)
            return;

        OnRender(screen);

        if (_children.Count == 0)
            return;

        if (ChildClipRectangle is { } clip)
        {
            using (screen.PushClip(clip))
            {
                RenderChildren(screen);
            }
        }
        else
        {
            RenderChildren(screen);
        }
    }

    private void RenderChildren(Screen screen)
    {
        foreach (var child in _children)
        {
            child.Render(screen);
        }
    }

    /// <summary>
    /// The area children are clipped to, or <c>null</c> to not clip them (the default).
    /// <see cref="Layout.Container"/> clips children to its bounds.
    /// </summary>
    protected virtual Rectangle? ChildClipRectangle => null;

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
    /// Called when the widget receives focus. Overrides must call the base implementation.
    /// </summary>
    protected internal virtual void OnFocus() => _hasFocus = true;

    /// <summary>
    /// Called when the widget loses focus. Overrides must call the base implementation.
    /// </summary>
    protected internal virtual void OnBlur() => _hasFocus = false;

    /// <summary>
    /// Called when a key is pressed.
    /// </summary>
    protected internal virtual bool OnKeyPress(ConsoleKeyInfo key) => false;

    /// <summary>
    /// Called when the left mouse button is pressed on this widget (by the default <see cref="OnMouse"/>).
    /// </summary>
    /// <returns><c>true</c> if the click was handled.</returns>
    protected internal virtual bool OnMouseClick(Point position) => false;

    /// <summary>
    /// Called for mouse events on this widget, or on a child that didn't handle them.
    /// The default calls <see cref="OnMouseClick"/> for a left-button press.
    /// </summary>
    /// <remarks>
    /// After a left-button press, the widget that received it also gets the following drag and
    /// release events, even when the pointer has moved off it.
    /// </remarks>
    /// <returns><c>true</c> if the event was handled; otherwise it goes to the parent.</returns>
    protected internal virtual bool OnMouse(MouseEvent e)
        => e.IsLeftPress && OnMouseClick(e.Position);

    /// <summary>
    /// Translates a wheel event into Up/Down arrow key presses (3 per notch), for widgets that
    /// already scroll with the arrow keys.
    /// </summary>
    /// <returns><c>true</c> if <paramref name="e"/> was a wheel event and a key press was handled.</returns>
    protected bool ScrollWithArrowKeys(MouseEvent e, int linesPerNotch = 3)
    {
        if (!e.IsWheel)
            return false;

        var key = e.Kind == MouseEventKind.WheelUp ? ConsoleKey.UpArrow : ConsoleKey.DownArrow;
        bool handled = false;
        for (int i = 0; i < linesPerNotch; i++)
            handled |= OnKeyPress(new ConsoleKeyInfo('\0', key, false, false, false));
        return handled;
    }

    /// <summary>
    /// Returns the topmost visible widget at <paramref name="point"/>: a child (last drawn first)
    /// or this widget, or <c>null</c>. Children are only considered inside the area they are
    /// clipped to.
    /// </summary>
    public virtual Widget? HitTest(Point point)
    {
        if (!Visible)
            return null;

        if (_children.Count > 0 && (ChildClipRectangle is not { } clip || clip.Contains(point)))
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                var hit = _children[i].HitTest(point);
                if (hit != null)
                    return hit;
            }
        }

        return Width > 0 && Height > 0 && Bounds.Contains(point) ? this : null;
    }
}
