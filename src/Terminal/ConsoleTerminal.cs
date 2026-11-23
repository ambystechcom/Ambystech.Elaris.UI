using System.Text;

namespace Ambystech.Elaris.UI.Terminal;

/// <summary>
/// An <see cref="ITerminal"/> backed by <see cref="Console"/>.
/// </summary>
public sealed class ConsoleTerminal : ITerminal
{
    private Encoding? _previousOutputEncoding;

    /// <inheritdoc />
    public int Width => Console.WindowWidth;

    /// <inheritdoc />
    public int Height => Console.WindowHeight;

    /// <inheritdoc />
    public bool TreatControlCAsInput
    {
        get => Console.TreatControlCAsInput;
        set => Console.TreatControlCAsInput = value;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        _previousOutputEncoding = Console.OutputEncoding;
        Console.OutputEncoding = Encoding.UTF8;
        TrySetCursorVisible(false);
    }

    /// <inheritdoc />
    public void Restore()
    {
        TrySetCursorVisible(true);

        if (_previousOutputEncoding != null)
        {
            try
            {
                Console.OutputEncoding = _previousOutputEncoding;
            }
            catch (IOException)
            {
            }
        }
    }

    /// <inheritdoc />
    public void Write(string text) => Console.Out.Write(text);

    /// <inheritdoc />
    public void Flush() => Console.Out.Flush();

    /// <inheritdoc />
    public bool TryReadKey(out ConsoleKeyInfo key)
    {
        try
        {
            if (Console.KeyAvailable)
            {
                key = Console.ReadKey(intercept: true);
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Input is redirected; there is no interactive keyboard.
        }

        key = default;
        return false;
    }

    private static void TrySetCursorVisible(bool visible)
    {
        try
        {
            Console.CursorVisible = visible;
        }
        catch (Exception ex) when (ex is IOException or PlatformNotSupportedException)
        {
            // Not supported by every terminal; the escape sequence already handles it.
        }
    }
}
