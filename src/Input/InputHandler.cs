namespace Ambystech.Elaris.UI.Input;

/// <summary>
/// Handles keyboard and mouse input from the console.
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
    /// Checks if a key is available without blocking.
    /// </summary>
    public bool IsKeyAvailable() => Console.KeyAvailable;
}
