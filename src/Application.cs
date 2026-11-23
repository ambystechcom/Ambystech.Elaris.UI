using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Ambystech.Elaris.UI.Input;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Widgets;

namespace Ambystech.Elaris.UI;

/// <summary>
/// Main application class that manages the event loop and rendering.
/// </summary>
public class Application
{
    private Screen? _screen;
    private Widget? _rootWidget;
    private Widget? _focusedWidget;
    private Widget? _initialFocusWidget;
    private volatile bool _running;
    private volatile bool _stopRequested;
    private CancellationTokenSource? _cancellationTokenSource;
    private InputHandler? _inputHandler;
    private bool _previousTreatControlCAsInput;
    private int _terminalRestored;
    private readonly List<PosixSignalRegistration> _signalRegistrations = [];

    /// <summary>
    /// Gets the current screen instance.
    /// </summary>
    public Screen? Screen => _screen;

    /// <summary>
    /// Gets or sets the target frames per second for rendering.
    /// </summary>
    public int TargetFps { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of queued key presses processed per frame.
    /// Bounds the work done in a single frame when a large paste arrives.
    /// </summary>
    public int MaxKeysPerFrame { get; set; } = 256;

    /// <summary>
    /// Gets or sets the key that stops the application when the focused widget does not handle it.
    /// Defaults to <see cref="ConsoleKey.Escape"/>. Set to <c>null</c> to disable.
    /// Widgets that use the key (for example, closing a menu or a suggestion popup) take precedence.
    /// </summary>
    public ConsoleKey? QuitKey { get; set; } = ConsoleKey.Escape;

    /// <summary>
    /// Gets or sets whether Ctrl+C stops the application. Defaults to <c>true</c>.
    /// </summary>
    public bool QuitOnCtrlC { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget that should receive initial focus.
    /// Set this before calling Run().
    /// </summary>
    public Widget? InitialFocusWidget
    {
        get => _initialFocusWidget;
        set => _initialFocusWidget = value;
    }

    /// <summary>
    /// Gets the widget that currently has keyboard focus.
    /// </summary>
    public Widget? FocusedWidget => _focusedWidget;

    internal bool StopRequested => _stopRequested;

    /// <summary>
    /// Runs the application with the specified root widget.
    /// </summary>
    public void Run(Widget rootWidget)
    {
        if (_running)
            throw new InvalidOperationException("Application is already running");

        ArgumentNullException.ThrowIfNull(rootWidget);

        _running = true;
        _stopRequested = false;
        _terminalRestored = 0;
        _cancellationTokenSource = new CancellationTokenSource();

        RegisterTerminalRestoreHandlers();
        try
        {
            Initialize(rootWidget);
            RunEventLoop();
        }
        finally
        {
            Shutdown();
            UnregisterTerminalRestoreHandlers();
        }
    }

    /// <summary>
    /// Stops the application.
    /// </summary>
    public void Stop()
    {
        _stopRequested = true;
        _running = false;
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already shut down.
        }
    }

    private void Initialize(Widget rootWidget)
    {
        int width = Console.WindowWidth;
        int height = Console.WindowHeight;

        _screen = new Screen(width, height);
        _screen.Initialize();

        try
        {
            _previousTreatControlCAsInput = Console.TreatControlCAsInput;
            // Receive Ctrl+C as a key press so the loop can shut down cleanly.
            Console.TreatControlCAsInput = true;
        }
        catch (IOException)
        {
            // Input is redirected; Ctrl+C will arrive through CancelKeyPress instead.
        }

        _inputHandler = new InputHandler(_cancellationTokenSource!.Token);

        AttachRoot(rootWidget, new Rectangle(0, 0, width, height));
    }

    /// <summary>
    /// Sets the root widget, its bounds and the initial focus without touching the console.
    /// </summary>
    internal void AttachRoot(Widget rootWidget, Rectangle bounds)
    {
        _rootWidget = rootWidget;
        _rootWidget.Bounds = bounds;

        var focusable = GetFocusableWidgets();

        if (_initialFocusWidget != null && focusable.Contains(_initialFocusWidget))
            _focusedWidget = _initialFocusWidget;
        else if (focusable.Count > 0)
            _focusedWidget = focusable[0];
        else
            _focusedWidget = _rootWidget;

        _focusedWidget.OnFocus();
    }

    private void RunEventLoop()
    {
        var stopwatch = new Stopwatch();

        while (_running && !_cancellationTokenSource!.Token.IsCancellationRequested)
        {
            stopwatch.Restart();

            ProcessInput();

            if (!_running)
                break;

            HandleResize();

            if (_screen != null && _rootWidget != null)
            {
                _screen.Clear();
                _rootWidget.Render(_screen);
                _screen.Render();
            }

            var frameTime = TimeSpan.FromMilliseconds(1000.0 / Math.Max(1, TargetFps));
            var sleepTime = frameTime - stopwatch.Elapsed;
            if (sleepTime > TimeSpan.Zero)
            {
                _cancellationTokenSource.Token.WaitHandle.WaitOne(sleepTime);
            }
        }
    }

    private void ProcessInput()
    {
        if (_inputHandler == null)
            return;

        for (int i = 0; i < MaxKeysPerFrame && _running; i++)
        {
            if (!_inputHandler.TryReadKey(out var key))
                break;

            HandleKey(key);
        }
    }

    /// <summary>
    /// Routes a key press: global shortcuts first, then the focused widget, then the quit key.
    /// </summary>
    internal void HandleKey(ConsoleKeyInfo key)
    {
        if (QuitOnCtrlC && key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            Stop();
            return;
        }

        if (key.Modifiers.HasFlag(ConsoleModifiers.Alt))
        {
            if (key.Key == ConsoleKey.RightArrow)
            {
                FocusNext(false);
                return;
            }

            if (key.Key == ConsoleKey.LeftArrow)
            {
                FocusNext(true);
                return;
            }
        }

        EnsureFocusIsValid();

        bool handled = _focusedWidget?.OnKeyPress(key) ?? false;

        if (!handled && QuitKey.HasValue && key.Key == QuitKey.Value && key.Modifiers == 0)
        {
            Stop();
        }
    }

    private void HandleResize()
    {
        if (_screen == null || _rootWidget == null)
            return;

        int currentWidth = Console.WindowWidth;
        int currentHeight = Console.WindowHeight;

        if (currentWidth != _screen.Width || currentHeight != _screen.Height)
        {
            _screen.Resize(currentWidth, currentHeight);
            _rootWidget.Bounds = new Rectangle(0, 0, currentWidth, currentHeight);
        }
    }

    private void Shutdown()
    {
        RestoreTerminal();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _running = false;
    }

    /// <summary>
    /// Restores the terminal state. Safe to call more than once and from any thread.
    /// </summary>
    private void RestoreTerminal()
    {
        if (Interlocked.Exchange(ref _terminalRestored, 1) == 1)
            return;

        try
        {
            _screen?.Shutdown();
        }
        catch (IOException)
        {
            // The terminal may already be gone.
        }

        try
        {
            Console.TreatControlCAsInput = _previousTreatControlCAsInput;
        }
        catch (IOException)
        {
        }
    }

    private void RegisterTerminalRestoreHandlers()
    {
        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // SIGTERM, SIGHUP and SIGQUIT would otherwise end the process without
        // leaving the alternate screen. Cancel the default action and let the
        // event loop exit normally so the terminal is restored.
        foreach (var signal in new[] { PosixSignal.SIGTERM, PosixSignal.SIGHUP, PosixSignal.SIGQUIT })
        {
            try
            {
                _signalRegistrations.Add(PosixSignalRegistration.Create(signal, OnTerminationSignal));
            }
            catch (PlatformNotSupportedException)
            {
                // Not available on this platform.
            }
        }
    }

    private void UnregisterTerminalRestoreHandlers()
    {
        Console.CancelKeyPress -= OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;

        foreach (var registration in _signalRegistrations)
            registration.Dispose();
        _signalRegistrations.Clear();
    }

    private void OnTerminationSignal(PosixSignalContext context)
    {
        context.Cancel = true;
        Stop();
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        if (QuitOnCtrlC)
        {
            // Let the event loop exit normally so the finally block restores the terminal.
            e.Cancel = true;
            Stop();
        }
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        Stop();
        RestoreTerminal();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        RestoreTerminal();
    }

    /// <summary>
    /// Sets the focused widget.
    /// </summary>
    public void SetFocus(Widget widget)
    {
        if (_focusedWidget == widget)
            return;

        _focusedWidget?.OnBlur();
        _focusedWidget = widget;
        _focusedWidget?.OnFocus();
    }

    /// <summary>
    /// Moves focus to the next (or previous) focusable widget.
    /// The focus order is recomputed each time, so widgets added, removed,
    /// hidden or disabled after <see cref="Run"/> are taken into account.
    /// </summary>
    public void FocusNext(bool reverse = false)
    {
        var focusable = GetFocusableWidgets();
        if (focusable.Count == 0)
            return;

        int currentIndex = _focusedWidget != null ? focusable.IndexOf(_focusedWidget) : -1;

        if (currentIndex < 0)
        {
            currentIndex = reverse ? focusable.Count - 1 : 0;
        }
        else if (reverse)
        {
            currentIndex = currentIndex == 0 ? focusable.Count - 1 : currentIndex - 1;
        }
        else
        {
            currentIndex = currentIndex == focusable.Count - 1 ? 0 : currentIndex + 1;
        }

        SetFocus(focusable[currentIndex]);
    }

    /// <summary>
    /// Returns the focusable widgets in tree order. Hidden or disabled widgets,
    /// and everything beneath them, are skipped.
    /// </summary>
    internal List<Widget> GetFocusableWidgets()
    {
        var focusable = new List<Widget>();
        if (_rootWidget != null)
            CollectFocusableWidgets(_rootWidget, focusable);
        return focusable;
    }

    private static void CollectFocusableWidgets(Widget widget, List<Widget> focusable)
    {
        if (!widget.Visible || !widget.Enabled)
            return;

        if (widget.IsFocusable)
            focusable.Add(widget);

        foreach (var child in widget.Children)
            CollectFocusableWidgets(child, focusable);
    }

    /// <summary>
    /// Moves focus away from a widget that was removed, hidden or disabled.
    /// </summary>
    private void EnsureFocusIsValid()
    {
        if (_rootWidget == null || _focusedWidget == null || _focusedWidget == _rootWidget)
            return;

        if (IsReachable(_focusedWidget))
            return;

        var focusable = GetFocusableWidgets();
        SetFocus(focusable.Count > 0 ? focusable[0] : _rootWidget);
    }

    private bool IsReachable(Widget widget)
    {
        for (var current = widget; current != null; current = current.Parent)
        {
            if (!current.Visible || !current.Enabled)
                return false;

            if (current == _rootWidget)
                return true;
        }

        return false;
    }
}
