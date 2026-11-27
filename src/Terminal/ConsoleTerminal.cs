using System.Text;
using Ambystech.Elaris.UI.Input;

namespace Ambystech.Elaris.UI.Terminal;

/// <summary>
/// An <see cref="ITerminal"/> backed by <see cref="Console"/>.
/// </summary>
public sealed class ConsoleTerminal : ITerminal
{
    private Encoding? _previousOutputEncoding;
    private readonly MouseSequenceDecoder _decoder;

    /// <summary>
    /// Creates a terminal backed by <see cref="Console"/>.
    /// </summary>
    public ConsoleTerminal()
    {
        _decoder = new MouseSequenceDecoder(IsKeyAvailable, () => Console.ReadKey(intercept: true));
    }

    /// <summary>
    /// <c>true</c> on macOS and Linux, where mouse reports arrive through standard input.
    /// On Windows, .NET reads input with <c>ReadConsoleInput</c> and drops mouse events, so
    /// mouse support is not available there yet.
    /// </summary>
    public bool SupportsMouse => !OperatingSystem.IsWindows();

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
        // Read through the decoder so keys it has looked ahead at are not lost.
        while (_decoder.TryRead(out var input))
        {
            if (!input.IsMouse)
            {
                key = input.Key;
                return true;
            }
        }

        key = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryReadInput(out TerminalInput input) => _decoder.TryRead(out input);

    private static bool IsKeyAvailable()
    {
        try
        {
            return Console.KeyAvailable;
        }
        catch (InvalidOperationException)
        {
            // Input is redirected; there is no interactive keyboard.
            return false;
        }
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
