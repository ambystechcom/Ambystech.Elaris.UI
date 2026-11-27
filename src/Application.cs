using System.Diagnostics;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Ambystech.Elaris.UI.Input;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Terminal;
using Ambystech.Elaris.UI.Widgets;

namespace Ambystech.Elaris.UI;

/// <summary>
/// Main application class that manages the event loop and rendering.
/// </summary>
public partial class Application
{
    private Screen? _screen;
    private Widget? _rootWidget;
    private Widget? _focusedWidget;
    private Widget? _initialFocusWidget;
    private volatile bool _running;
    private volatile bool _stopRequested;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ITerminal _terminal;
    private bool _previousTreatControlCAsInput;
    private int _terminalRestored;
    private readonly List<PosixSignalRegistration> _signalRegistrations = [];
    private readonly AutoResetEvent _wake = new(false);
    private int _renderRequested;
    private bool _isRendering;
    private ExceptionDispatchInfo? _fatalException;
    private bool _mouseReportingOn;
    private Widget? _mouseCapture;
    private MouseEvent? _lastLeftPress;
    private long _lastLeftPressTicks;

    private const string EnableMouseSequence = "\x1b[?1000h\x1b[?1002h\x1b[?1006h";
    private const string DisableMouseSequence = "\x1b[?1006l\x1b[?1002l\x1b[?1000l";

    /// <summary>
    /// Two left clicks at the same position within this time count as a double click.
    /// </summary>
    public TimeSpan DoubleClickTime { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>
    /// Gets or sets the color mode, overriding what the terminal reports
    /// (<see cref="ITerminal.ColorMode"/>). When <c>null</c> (the default), the terminal's mode is
    /// used: on the console it is detected from <c>NO_COLOR</c>, <c>COLORTERM</c> and <c>TERM</c>.
    /// Can be changed while the application runs.
    /// </summary>
    public Rendering.ColorMode? ColorMode
    {
        get => _colorMode;
        set
        {
            _colorMode = value;
            if (_screen != null)
            {
                _screen.ColorMode = value ?? _terminal.ColorMode;
                RequestRender();
            }
        }
    }

    private Rendering.ColorMode? _colorMode;

    /// <summary>
    /// Gets or sets whether to turn on mouse reporting when the terminal supports it
    /// (<see cref="ITerminal.SupportsMouse"/>). Defaults to <c>true</c>. Set before <see cref="Run"/>.
    /// </summary>
    /// <remarks>
    /// While mouse reporting is on, most terminals select text only with Shift held down.
    /// </remarks>
    public bool MouseEnabled { get; set; } = true;

    /// <summary>
    /// Creates an application that runs in the console.
    /// </summary>
    public Application()
        : this(new ConsoleTerminal())
    {
    }

    /// <summary>
    /// Creates an application that runs in the given terminal.
    /// Pass a <see cref="VirtualTerminal"/> to run an application in tests.
    /// </summary>
    public Application(ITerminal terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    /// <summary>
    /// Gets the terminal the application runs in.
    /// </summary>
    public ITerminal Terminal => _terminal;

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

    /// <summary>
    /// Gets or sets the theme for the whole application. Applied to the root widget; can be
    /// changed while the application runs. When <c>null</c>, widgets use their own
    /// <see cref="Widget.Theme"/> or <see cref="UI.Theme.Dark"/>.
    /// </summary>
    public Theme? Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            if (_rootWidget != null && value != null)
                _rootWidget.Theme = value;
        }
    }

    private Theme? _theme;

    /// <summary>
    /// Gets or sets whether to redraw every frame, even when nothing asked for a redraw.
    /// Defaults to <c>false</c>: the screen is redrawn after input, queued actions, resizes and
    /// <see cref="Widget.Invalidate"/>. Turn this on for code that changes widgets from other
    /// threads without <see cref="Post"/>, <see cref="InvokeAsync(Action)"/> or
    /// <see cref="Widget.Invalidate"/>.
    /// </summary>
    public bool AlwaysRender { get; set; }

    internal bool StopRequested => _stopRequested;

    /// <summary>
    /// Number of frames drawn by the current or last run.
    /// </summary>
    internal int RenderCount { get; private set; }

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
        _fatalException = null;
        RenderCount = 0;
        _cancellationTokenSource = new CancellationTokenSource();
        _uiThreadId = Environment.CurrentManagedThreadId;

        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new ElarisSynchronizationContext(this));

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
            SynchronizationContext.SetSynchronizationContext(previousContext);
            _uiThreadId = -1;
            rootWidget.RenderRequested = null;
            CancelQueuedActions();
        }

        _fatalException?.Throw();
    }

    /// <summary>
    /// Stops the application.
    /// </summary>
    public void Stop()
    {
        _stopRequested = true;
        _running = false;
        _wake.Set();
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
        int width = _terminal.Width;
        int height = _terminal.Height;

        _screen = new Screen(width, height, _terminal) { ColorMode = _colorMode ?? _terminal.ColorMode };
        _screen.Initialize();

        _mouseReportingOn = MouseEnabled && _terminal.SupportsMouse;
        if (_mouseReportingOn)
        {
            _terminal.Write(EnableMouseSequence);
            _terminal.Flush();
        }

        try
        {
            _previousTreatControlCAsInput = _terminal.TreatControlCAsInput;
            // Receive Ctrl+C as a key press so the loop can shut down cleanly.
            _terminal.TreatControlCAsInput = true;
        }
        catch (IOException)
        {
            // Input is redirected; Ctrl+C will arrive through CancelKeyPress instead.
        }

        AttachRoot(rootWidget, new Rectangle(0, 0, width, height));
    }

    /// <summary>
    /// Sets the root widget, its bounds and the initial focus without touching the console.
    /// </summary>
    internal void AttachRoot(Widget rootWidget, Rectangle bounds)
    {
        _rootWidget = rootWidget;
        _rootWidget.RenderRequested = RequestRender;
        if (_theme != null)
            _rootWidget.Theme = _theme;
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

    /// <summary>
    /// Asks for a redraw on the next frame. Thread-safe. Ignored while a frame is being drawn,
    /// so layout changes made during rendering don't cause endless redraws.
    /// </summary>
    private void RequestRender()
    {
        if (_isRendering && CheckAccess())
            return;

        if (Interlocked.Exchange(ref _renderRequested, 1) == 0)
            _wake.Set();
    }

    private void RunEventLoop()
    {
        var sinceLastRender = Stopwatch.StartNew();
        bool needsRender = true;
        var token = _cancellationTokenSource!.Token;
        WaitHandle[] waitHandles = [token.WaitHandle, _wake];

        while (_running && !token.IsCancellationRequested)
        {
            needsRender |= ProcessInput();
            if (!_running)
                break;

            needsRender |= RunQueuedActions();
            if (!_running)
                break;

            needsRender |= HandleResize();
            needsRender |= Interlocked.Exchange(ref _renderRequested, 0) == 1;
            needsRender |= AlwaysRender;

            var frameTime = TimeSpan.FromMilliseconds(1000.0 / Math.Max(1, TargetFps));

            if (needsRender && (RenderCount == 0 || sinceLastRender.Elapsed >= frameTime))
            {
                RenderFrame();
                sinceLastRender.Restart();
                needsRender = false;
            }

            // Keys can't wake the loop, so poll once per frame. Queued actions and
            // Invalidate() wake it early. A pending redraw waits out the rest of the frame.
            var wait = needsRender ? frameTime - sinceLastRender.Elapsed : frameTime;
            if (wait > TimeSpan.Zero)
                WaitHandle.WaitAny(waitHandles, wait);
        }
    }

    private void RenderFrame()
    {
        if (_screen == null || _rootWidget == null)
            return;

        _isRendering = true;
        try
        {
            _screen.Clear();

            var theme = _rootWidget.EffectiveTheme;
            _screen.BaseBackground = theme.Background;
            if (theme.Background.A > 0)
            {
                _screen.FillRectangle(new Rectangle(0, 0, _screen.Width, _screen.Height), ' ', theme.Foreground, theme.Background);
            }

            _rootWidget.Render(_screen);
            _screen.Render();
            RenderCount++;
        }
        finally
        {
            _isRendering = false;
        }
    }

    /// <returns><c>true</c> if any key was read.</returns>
    private bool ProcessInput()
    {
        bool any = false;

        for (int i = 0; i < MaxKeysPerFrame && _running; i++)
        {
            if (!_terminal.TryReadInput(out var input))
                break;

            any = true;
            if (input.Mouse is { } mouse)
                RunOnUiThread(() => HandleMouse(mouse));
            else
                RunOnUiThread(() => HandleKey(input.Key));
        }

        return any;
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

    /// <summary>
    /// Routes a mouse event: finds the widget under the pointer (or the widget holding the mouse
    /// since a left press), moves focus on a left press, then offers the event to the widget
    /// and its parents until one handles it.
    /// </summary>
    internal void HandleMouse(MouseEvent e)
    {
        if (_rootWidget == null)
            return;

        if (e.IsLeftPress)
            e = e with { ClickCount = CountClicks(e) };

        bool captured = _mouseCapture != null
            && e.Kind is MouseEventKind.Drag or MouseEventKind.Release;

        var target = captured ? _mouseCapture : _rootWidget.HitTest(e.Position);

        if (e.IsLeftPress)
        {
            _mouseCapture = target;

            for (var w = target; w != null; w = w.Parent)
            {
                if (w.IsFocusable && w.Enabled)
                {
                    SetFocus(w);
                    break;
                }
            }
        }

        try
        {
            for (var w = target; w != null; w = w.Parent)
            {
                if (w.Enabled && w.OnMouse(e))
                    break;
            }
        }
        finally
        {
            if (e.Kind == MouseEventKind.Release)
                _mouseCapture = null;
        }
    }

    private int CountClicks(MouseEvent press)
    {
        long now = Environment.TickCount64;
        int count = 1;

        if (_lastLeftPress is { } last
            && last.X == press.X && last.Y == press.Y
            && now - _lastLeftPressTicks <= (long)DoubleClickTime.TotalMilliseconds)
        {
            count = Math.Min(last.ClickCount + 1, 3);
        }

        _lastLeftPress = press with { ClickCount = count };
        _lastLeftPressTicks = now;
        return count;
    }

    /// <returns><c>true</c> if the terminal size changed.</returns>
    private bool HandleResize()
    {
        if (_screen == null || _rootWidget == null)
            return false;

        int currentWidth = _terminal.Width;
        int currentHeight = _terminal.Height;

        if (currentWidth != _screen.Width || currentHeight != _screen.Height)
        {
            _screen.Resize(currentWidth, currentHeight);
            _rootWidget.Bounds = new Rectangle(0, 0, currentWidth, currentHeight);
            return true;
        }

        return false;
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
            if (_mouseReportingOn)
            {
                _terminal.Write(DisableMouseSequence);
                _mouseReportingOn = false;
            }

            _screen?.Shutdown();
        }
        catch (IOException)
        {
            // The terminal may already be gone.
        }

        try
        {
            _terminal.TreatControlCAsInput = _previousTreatControlCAsInput;
        }
        catch (IOException)
        {
        }
    }

    private void RegisterTerminalRestoreHandlers()
    {
        // Process-wide handlers only make sense when drawing to the real console.
        if (_terminal is not ConsoleTerminal)
            return;

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
