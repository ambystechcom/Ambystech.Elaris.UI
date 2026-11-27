using Ambystech.Elaris.UI.Input;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Terminal;

/// <summary>
/// The terminal that an <see cref="Application"/> and its <see cref="Rendering.Screen"/>
/// draw to and read keys from.
/// </summary>
/// <remarks>
/// <see cref="ConsoleTerminal"/> talks to the real console. <see cref="VirtualTerminal"/>
/// keeps everything in memory, which makes it possible to test rendering and input
/// without a console.
/// </remarks>
public interface ITerminal
{
    /// <summary>
    /// Gets the terminal width in columns.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the terminal height in rows.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets or sets whether Ctrl+C is delivered as a key press instead of a signal.
    /// </summary>
    bool TreatControlCAsInput { get; set; }

    /// <summary>
    /// Prepares the terminal for drawing (for example, setting UTF-8 output).
    /// Escape sequences such as switching to the alternate screen are written separately
    /// by <see cref="Rendering.Screen"/>.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Undoes what <see cref="Initialize"/> changed.
    /// </summary>
    void Restore();

    /// <summary>
    /// Writes text, which may contain ANSI escape sequences.
    /// </summary>
    void Write(string text);

    /// <summary>
    /// Flushes any buffered output.
    /// </summary>
    void Flush();

    /// <summary>
    /// Reads a key press if one is waiting, without blocking.
    /// </summary>
    /// <returns><c>true</c> if a key was read; otherwise <c>false</c>.</returns>
    bool TryReadKey(out ConsoleKeyInfo key);

    /// <summary>
    /// Gets how many colors the terminal can show. Defaults to <see cref="Rendering.ColorMode.TrueColor"/>.
    /// </summary>
    ColorMode ColorMode => ColorMode.TrueColor;

    /// <summary>
    /// Gets whether this terminal can report mouse events through <see cref="TryReadInput"/>.
    /// Defaults to <c>false</c>.
    /// </summary>
    bool SupportsMouse => false;

    /// <summary>
    /// Reads a key press or mouse event if one is waiting, without blocking.
    /// The default implementation returns keys from <see cref="TryReadKey"/>.
    /// </summary>
    /// <returns><c>true</c> if an input was read; otherwise <c>false</c>.</returns>
    bool TryReadInput(out TerminalInput input)
    {
        if (TryReadKey(out var key))
        {
            input = TerminalInput.FromKey(key);
            return true;
        }

        input = default;
        return false;
    }
}
