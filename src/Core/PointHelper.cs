using System.Drawing;

namespace Ambystech.Elaris.UI.Core;

/// <summary>
/// Helper methods and extension methods for System.Drawing.Point.
/// </summary>
public static class PointHelper
{
    /// <summary>
    /// Deconstructs a Point into its X and Y components for pattern matching.
    /// </summary>
    public static void Deconstruct(this Point point, out int x, out int y)
    {
        x = point.X;
        y = point.Y;
    }

    /// <summary>
    /// Gets a point at the origin (0, 0).
    /// </summary>
    public static Point Zero => Point.Empty;
}
