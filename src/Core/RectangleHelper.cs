using System.Drawing;

namespace Ambystech.Elaris.UI.Core;

/// <summary>
/// Helper methods and extension methods for System.Drawing.Rectangle.
/// </summary>
public static class RectangleHelper
{
    /// <summary>
    /// Gets the position (top-left corner) of the rectangle as a Point.
    /// </summary>
    public static Point Position(this Rectangle rect) => new(rect.X, rect.Y);

    /// <summary>
    /// Gets the top-left corner of the rectangle.
    /// </summary>
    public static Point TopLeft(this Rectangle rect) => new(rect.X, rect.Y);

    /// <summary>
    /// Gets the top-right corner of the rectangle.
    /// </summary>
    public static Point TopRight(this Rectangle rect) => new(rect.Right, rect.Y);

    /// <summary>
    /// Gets the bottom-left corner of the rectangle.
    /// </summary>
    public static Point BottomLeft(this Rectangle rect) => new(rect.X, rect.Bottom);

    /// <summary>
    /// Gets the bottom-right corner of the rectangle.
    /// </summary>
    public static Point BottomRight(this Rectangle rect) => new(rect.Right, rect.Bottom);
}
