using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Layout;

/// <summary>
/// A container widget that can hold and layout child widgets.
/// </summary>
public class Container : Widget
{
    private LayoutMode _layoutMode = LayoutMode.Absolute;
    private int _padding = 0;
    private int _spacing = 0;

    /// <summary>
    /// Gets or sets the layout mode for child widgets.
    /// </summary>
    public LayoutMode LayoutMode
    {
        get => _layoutMode;
        set
        {
            if (_layoutMode != value)
            {
                _layoutMode = value;
                LayoutChildren();
            }
        }
    }

    /// <summary>
    /// Gets or sets the padding around child widgets.
    /// </summary>
    public int Padding
    {
        get => _padding;
        set
        {
            if (_padding != value)
            {
                _padding = Math.Max(0, value);
                LayoutChildren();
            }
        }
    }

    /// <summary>
    /// Gets or sets the spacing between child widgets in flow layouts.
    /// </summary>
    public int Spacing
    {
        get => _spacing;
        set
        {
            if (_spacing != value)
            {
                _spacing = Math.Max(0, value);
                LayoutChildren();
            }
        }
    }

    public Container() => BackgroundColor = Color.Transparent;

    protected override void OnRender(Screen screen)
    {
        // Container itself doesn't render anything
        // Children will be rendered by the base Widget.Render() method
    }

    protected override void OnBoundsChanged()
    {
        base.OnBoundsChanged();
        LayoutChildren();
    }

    protected override void OnChildAdded(Widget child)
    {
        base.OnChildAdded(child);
        LayoutChildren();
    }

    protected override void OnChildRemoved(Widget child)
    {
        base.OnChildRemoved(child);
        LayoutChildren();
    }

    /// <summary>
    /// Layouts children based on the current layout mode.
    /// </summary>
    private void LayoutChildren()
    {
        if (Children.Count == 0)
            return;

        switch (_layoutMode)
        {
            case LayoutMode.Absolute:
                break;

            case LayoutMode.Vertical:
                LayoutVertical();
                break;

            case LayoutMode.Horizontal:
                LayoutHorizontal();
                break;

            case LayoutMode.Fill:
                LayoutFill();
                break;
        }
    }

    private void LayoutVertical()
    {
        int currentY = Y + _padding;
        int availableWidth = Width - _padding * 2;

        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            child.X = X + _padding;
            child.Y = currentY;
            child.Width = availableWidth;

            currentY += child.Height + _spacing;

            if (currentY >= Y + Height - _padding)
                break;
        }
    }

    private void LayoutHorizontal()
    {
        int currentX = X + _padding;
        int availableHeight = Height - _padding * 2;

        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            child.X = currentX;
            child.Y = Y + _padding;
            child.Height = availableHeight;

            currentX += child.Width + _spacing;

            if (currentX >= X + Width - _padding)
                break;
        }
    }

    private void LayoutFill()
    {
        int contentX = X + _padding;
        int contentY = Y + _padding;
        int contentWidth = Width - _padding * 2;
        int contentHeight = Height - _padding * 2;

        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            child.X = contentX;
            child.Y = contentY;
            child.Width = contentWidth;
            child.Height = contentHeight;
        }
    }
}
