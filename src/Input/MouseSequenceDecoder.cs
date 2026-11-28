using System.Text;

namespace Ambystech.Elaris.UI.Input;

/// <summary>
/// Turns a stream of key presses into keys and mouse events.
/// </summary>
/// <remarks>
/// With SGR mouse reporting on (<c>CSI ?1006 h</c>), terminals send mouse events as
/// <c>ESC [ &lt; button ; column ; row M</c> (press, drag, wheel) or <c>… m</c> (release).
/// <see cref="Console.ReadKey(bool)"/> on Unix does not recognize these and returns them as
/// separate keys (Escape, '[', '&lt;', digits…). This decoder reassembles them. Keys that turn
/// out not to be a mouse sequence are returned unchanged, in order. A lone Escape is only
/// treated as the start of a sequence if the next key is already available.
/// </remarks>
internal sealed class MouseSequenceDecoder(Func<bool> isKeyAvailable, Func<ConsoleKeyInfo> readKey)
{
    private const int MaxSequenceLength = 24;

    /// <summary>How long to wait for the rest of a sequence once <c>ESC [ &lt;</c> has been seen.</summary>
    private static readonly TimeSpan SequenceTimeout = TimeSpan.FromMilliseconds(50);

    private readonly Queue<ConsoleKeyInfo> _pending = new();

    public bool TryRead(out TerminalInput input)
    {
        if (!TryNext(out var key))
        {
            input = default;
            return false;
        }

        if (key.Key == ConsoleKey.Escape && key.KeyChar == '\x1b' && TryReadMouse(out var mouse))
        {
            input = TerminalInput.FromMouse(mouse);
            return true;
        }

        input = TerminalInput.FromKey(key);
        return true;
    }

    private bool TryNext(out ConsoleKeyInfo key)
    {
        if (_pending.TryDequeue(out key))
            return true;

        if (isKeyAvailable())
        {
            key = readKey();
            return true;
        }

        key = default;
        return false;
    }

    /// <summary>
    /// Called after an Escape. Reads the rest of an SGR mouse sequence if one follows;
    /// otherwise puts back what it read so those keys are returned normally.
    /// </summary>
    private bool TryReadMouse(out MouseEvent mouse)
    {
        mouse = default;
        var consumed = new List<ConsoleKeyInfo>();

        bool Expect(char expected, bool wait)
        {
            if (!TryNextWithin(wait ? SequenceTimeout : TimeSpan.Zero, out var next))
                return false;

            consumed.Add(next);
            return next.KeyChar == expected;
        }

        // Only look ahead if the bytes are already there: a real Escape key press is not delayed.
        if (!Expect('[', wait: false) || !Expect('<', wait: true))
        {
            PutBack(consumed);
            return false;
        }

        var body = new StringBuilder();
        while (body.Length < MaxSequenceLength)
        {
            if (!TryNextWithin(SequenceTimeout, out var next))
                break;

            consumed.Add(next);
            char c = next.KeyChar;

            if (c is 'M' or 'm')
            {
                if (TryParseSgr(body.ToString(), c, out mouse))
                    return true;
                break;
            }

            if (!(char.IsAsciiDigit(c) || c == ';'))
                break;

            body.Append(c);
        }

        PutBack(consumed);
        return false;
    }

    private bool TryNextWithin(TimeSpan timeout, out ConsoleKeyInfo key)
    {
        if (TryNext(out key))
            return true;

        if (timeout <= TimeSpan.Zero)
            return false;

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(1);
            if (TryNext(out key))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Puts keys back in front of anything already pending, keeping their order.
    /// </summary>
    private void PutBack(List<ConsoleKeyInfo> keys)
    {
        if (keys.Count == 0)
            return;

        var rest = _pending.ToArray();
        _pending.Clear();
        foreach (var k in keys)
            _pending.Enqueue(k);
        foreach (var k in rest)
            _pending.Enqueue(k);
    }

    /// <summary>
    /// Parses the body of an SGR mouse sequence (<c>button;column;row</c>) and its final
    /// character (<c>M</c> press/drag/wheel, <c>m</c> release). Columns and rows become 0-based.
    /// </summary>
    internal static bool TryParseSgr(string body, char final, out MouseEvent mouse)
    {
        mouse = default;
        var parts = body.Split(';');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int code)
            || !int.TryParse(parts[1], out int column)
            || !int.TryParse(parts[2], out int row)
            || column < 1 || row < 1)
        {
            return false;
        }

        ConsoleModifiers modifiers = 0;
        if ((code & 4) != 0) modifiers |= ConsoleModifiers.Shift;
        if ((code & 8) != 0) modifiers |= ConsoleModifiers.Alt;
        if ((code & 16) != 0) modifiers |= ConsoleModifiers.Control;

        bool motion = (code & 32) != 0;
        bool wheel = (code & 64) != 0;
        int button = code & 3;

        MouseEventKind kind;
        MouseButton mouseButton;

        if (wheel)
        {
            // 64/65 are vertical; 66/67 (horizontal) are ignored.
            if (button > 1)
                return false;

            kind = button == 0 ? MouseEventKind.WheelUp : MouseEventKind.WheelDown;
            mouseButton = MouseButton.None;
        }
        else
        {
            mouseButton = button switch
            {
                0 => MouseButton.Left,
                1 => MouseButton.Middle,
                2 => MouseButton.Right,
                _ => MouseButton.None,
            };

            // Motion without a button (any-event tracking) is not used.
            if (mouseButton == MouseButton.None)
                return false;

            kind = final == 'm' ? MouseEventKind.Release
                : motion ? MouseEventKind.Drag
                : MouseEventKind.Press;
        }

        mouse = new MouseEvent(column - 1, row - 1, mouseButton, kind, modifiers);
        return true;
    }
}
