using System.Drawing;
using Ambystech.Elaris.UI.Core;

namespace Ambystech.Elaris.UI.Rendering;

/// <summary>
/// Represents a single character cell in the screen buffer.
/// </summary>
public struct Cell(char character, Color foreground, Color background,
    bool bold = false, bool italic = false, bool underline = false) : IEquatable<Cell>
{
    public char Character { get; set; } = character;
    public Color Foreground { get; set; } = foreground;
    public Color Background { get; set; } = background;
    public bool Bold { get; set; } = bold;
    public bool Italic { get; set; } = italic;
    public bool Underline { get; set; } = underline;

    /// <summary>
    /// Compares two cells by how they look: colors are compared by ARGB value (so a named
    /// color equals the same color built with <see cref="Color.FromArgb(int, int, int, int)"/>),
    /// and all fully transparent colors are equal.
    /// </summary>
    public bool Equals(Cell other)
        => Character == other.Character
           && ColorKey(Foreground) == ColorKey(other.Foreground)
           && ColorKey(Background) == ColorKey(other.Background)
           && Bold == other.Bold
           && Italic == other.Italic
           && Underline == other.Underline;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Cell cell && Equals(cell);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Character, ColorKey(Foreground), ColorKey(Background), Bold, Italic, Underline);

    /// <summary>
    /// ARGB value of a color, with every fully transparent color mapped to 0.
    /// </summary>
    internal static int ColorKey(Color color) => color.A == 0 ? 0 : color.ToArgb();

    /// <summary>Compares two cells with <see cref="Equals(Cell)"/>.</summary>
    public static bool operator ==(Cell left, Cell right) => left.Equals(right);
    /// <summary>Compares two cells with <see cref="Equals(Cell)"/>.</summary>
    public static bool operator !=(Cell left, Cell right) => !left.Equals(right);

    /// <summary>A blank cell: a space with white text on a transparent background.</summary>
    public static readonly Cell Empty = new(' ', Color.White, Color.Transparent);
}
