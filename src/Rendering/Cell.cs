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

    public bool Equals(Cell other)
        => Character == other.Character
           && Foreground.Equals(other.Foreground)
           && Background.Equals(other.Background)
           && Bold == other.Bold
           && Italic == other.Italic
           && Underline == other.Underline;

    public override bool Equals(object? obj) => obj is Cell cell && Equals(cell);
    public override int GetHashCode() => HashCode.Combine(Character, Foreground, Background, Bold, Italic, Underline);

    public static bool operator ==(Cell left, Cell right) => left.Equals(right);
    public static bool operator !=(Cell left, Cell right) => !left.Equals(right);

    public static readonly Cell Empty = new(' ', Color.White, Color.Transparent);
}
