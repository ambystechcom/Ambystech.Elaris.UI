using System.Drawing;

namespace Ambystech.Elaris.UI.Input;

/// <summary>
/// A mouse button.
/// </summary>
public enum MouseButton
{
    /// <summary>No button (wheel events).</summary>
    None,

    /// <summary>The left (primary) button.</summary>
    Left,

    /// <summary>The middle button.</summary>
    Middle,

    /// <summary>The right (secondary) button.</summary>
    Right,
}

/// <summary>
/// What happened in a <see cref="MouseEvent"/>.
/// </summary>
public enum MouseEventKind
{
    /// <summary>A button was pressed.</summary>
    Press,

    /// <summary>A button was released.</summary>
    Release,

    /// <summary>The mouse moved while a button was held down.</summary>
    Drag,

    /// <summary>The wheel was scrolled up (away from the user).</summary>
    WheelUp,

    /// <summary>The wheel was scrolled down (toward the user).</summary>
    WheelDown,
}

/// <summary>
/// A mouse event at a screen position (0-based columns and rows).
/// </summary>
/// <param name="X">Column, starting at 0.</param>
/// <param name="Y">Row, starting at 0.</param>
/// <param name="Button">The button involved; <see cref="MouseButton.None"/> for wheel events.</param>
/// <param name="Kind">What happened.</param>
/// <param name="Modifiers">Shift, Alt and Control state.</param>
/// <param name="ClickCount">For presses: 1 for a single click, 2 for a double click, 3 for a triple click.</param>
public readonly record struct MouseEvent(
    int X,
    int Y,
    MouseButton Button,
    MouseEventKind Kind,
    ConsoleModifiers Modifiers = 0,
    int ClickCount = 1)
{
    /// <summary>Gets the position as a point.</summary>
    public Point Position => new(X, Y);

    /// <summary>True for a press of the left button.</summary>
    public bool IsLeftPress => Kind == MouseEventKind.Press && Button == MouseButton.Left;

    /// <summary>True for a release of the left button.</summary>
    public bool IsLeftRelease => Kind == MouseEventKind.Release && Button == MouseButton.Left;

    /// <summary>True for wheel up or down.</summary>
    public bool IsWheel => Kind is MouseEventKind.WheelUp or MouseEventKind.WheelDown;
}

/// <summary>
/// One input from the terminal: a key press or a mouse event.
/// </summary>
public readonly struct TerminalInput
{
    private TerminalInput(ConsoleKeyInfo key, MouseEvent? mouse)
    {
        Key = key;
        Mouse = mouse;
    }

    /// <summary>Gets the key, when <see cref="IsMouse"/> is false.</summary>
    public ConsoleKeyInfo Key { get; }

    /// <summary>Gets the mouse event, or null for a key press.</summary>
    public MouseEvent? Mouse { get; }

    /// <summary>Gets whether this is a mouse event.</summary>
    public bool IsMouse => Mouse.HasValue;

    /// <summary>Creates a key input.</summary>
    public static TerminalInput FromKey(ConsoleKeyInfo key) => new(key, null);

    /// <summary>Creates a mouse input.</summary>
    public static TerminalInput FromMouse(MouseEvent mouse) => new(default, mouse);
}
