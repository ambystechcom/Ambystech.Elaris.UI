using Ambystech.Elaris.UI.Enums;

namespace Ambystech.Elaris.UI.Widgets.Layout.Responsive;

/// <summary>
/// A container that automatically adjusts widget positions when the window is resized.
/// </summary>
public class ResponsiveContainer : Container
{
    private Action<int, int>? _layoutCallback;

    public ResponsiveContainer() => LayoutMode = LayoutMode.Absolute;

    /// <summary>
    /// Sets the callback that will be invoked when the container is resized.
    /// The callback receives the new width and height.
    /// </summary>
    public void OnResize(Action<int, int> layoutCallback) => _layoutCallback = layoutCallback;

    protected override void OnBoundsChanged()
    {
        base.OnBoundsChanged();

        // Invoke the layout callback when bounds change
        _layoutCallback?.Invoke(Width, Height);
    }
}
