namespace Ambystech.Elaris.UI.Input;

/// <summary>
/// Reads keyboard input from the console.
/// </summary>
public class InputHandler(CancellationToken cancellationToken = default)
{
    private readonly CancellationToken _cancellationToken = cancellationToken;

    /// <summary>
    /// Reads a key press asynchronously.
    /// Returns null if cancelled or no input available.
    /// </summary>
    public async Task<ConsoleKeyInfo?> ReadKeyAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                while (!Console.KeyAvailable)
                {
                    if (_cancellationToken.IsCancellationRequested)
                        return null;

                    Thread.Sleep(10);
                }

                return (ConsoleKeyInfo?)Console.ReadKey(intercept: true);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }, _cancellationToken);
    }

    /// <summary>
    /// Reads a key press if one is waiting, without blocking.
    /// </summary>
    /// <returns><c>true</c> if a key was read; otherwise <c>false</c>.</returns>
    public bool TryReadKey(out ConsoleKeyInfo key)
    {
        if (!_cancellationToken.IsCancellationRequested && IsKeyAvailable())
        {
            key = Console.ReadKey(intercept: true);
            return true;
        }

        key = default;
        return false;
    }

    /// <summary>
    /// Checks if a key is available without blocking.
    /// </summary>
    public bool IsKeyAvailable()
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
}
