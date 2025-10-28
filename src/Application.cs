using System.Drawing;
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
    private bool _running;
    private CancellationTokenSource? _cancellationTokenSource;
    private InputHandler? _inputHandler;
    private List<Widget> _focusableWidgets = [];

    /// <summary>
    /// Gets the current screen instance.
    /// </summary>
    public Screen? Screen => _screen;

    /// <summary>
    /// Gets or sets the target frames per second for rendering.
    /// </summary>
    public int TargetFps { get; set; } = 30;

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
    /// Runs the application with the specified root widget.
    /// </summary>
    public void Run(Widget rootWidget)
    {
        if (_running)
            throw new InvalidOperationException("Application is already running");

        _rootWidget = rootWidget ?? throw new ArgumentNullException(nameof(rootWidget));
        _running = true;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            Initialize();
            RunEventLoop();
        }
        finally
        {
            Shutdown();
        }
    }

    /// <summary>
    /// Stops the application.
    /// </summary>
    public void Stop()
    {
        _running = false;
        _cancellationTokenSource?.Cancel();
    }

    private void Initialize()
    {
        int width = Console.WindowWidth;
        int height = Console.WindowHeight;

        _screen = new Screen(width, height);
        _screen.Initialize();

        _inputHandler = new InputHandler(_cancellationTokenSource!.Token);

        if (_rootWidget != null)
        {
            _rootWidget.Bounds = new Rectangle(0, 0, width, height);

            _focusableWidgets = GetFocusableWidgets(_rootWidget);

            if (_initialFocusWidget != null && _focusableWidgets.Contains(_initialFocusWidget))
            {
                _focusedWidget = _initialFocusWidget;
            }
            else if (_focusableWidgets.Any())
            {
                _focusedWidget = _focusableWidgets.First();
            }
            else
            {
                _focusedWidget = _rootWidget;
            }

            _focusedWidget.OnFocus();
        }
    }

    private void RunEventLoop()
    {
        var frameTime = TimeSpan.FromMilliseconds(1000.0 / TargetFps);
        var lastFrameTime = DateTime.UtcNow;

        while (_running && !_cancellationTokenSource!.Token.IsCancellationRequested)
        {
            var currentTime = DateTime.UtcNow;
            var elapsed = currentTime - lastFrameTime;

            ProcessInput();

            HandleResize();

            if (_screen != null && _rootWidget != null)
            {
                _screen.Clear();
                _rootWidget.Render(_screen);
                _screen.Render();
            }

            lastFrameTime = currentTime;

            var sleepTime = frameTime - elapsed;
            if (sleepTime > TimeSpan.Zero)
            {
                Thread.Sleep(sleepTime);
            }
        }
    }

    private void ProcessInput()
    {
        if (_inputHandler == null || !_inputHandler.IsKeyAvailable())
            return;

        var keyTask = _inputHandler.ReadKeyAsync();
        keyTask.Wait(TimeSpan.FromMilliseconds(10));

        if (keyTask.IsCompleted && keyTask.Result.HasValue)
        {
            var key = keyTask.Result.Value;

            if (key.Key == ConsoleKey.Escape || (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control)))
            {
                Stop();
                return;
            }

            if (key.Key == ConsoleKey.Tab)
            {
                FocusNext(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
                return;
            }

            _focusedWidget?.OnKeyPress(key);
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
        _screen?.Shutdown();
        _cancellationTokenSource?.Dispose();
        _running = false;
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

    private void FocusNext(bool reverse = false)
    {
        if (_focusableWidgets.Count == 0)
            return;

        int currentIndex = _focusedWidget != null ? _focusableWidgets.IndexOf(_focusedWidget) : -1;

        if (reverse)
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = _focusableWidgets.Count - 1;
        }
        else
        {
            currentIndex++;
            if (currentIndex >= _focusableWidgets.Count)
                currentIndex = 0;
        }

        SetFocus(_focusableWidgets[currentIndex]);
    }

    private List<Widget> GetFocusableWidgets(Widget root)
    {
        var focusable = new List<Widget>();
        CollectFocusableWidgets(root, focusable);
        return focusable;
    }

    private void CollectFocusableWidgets(Widget widget, List<Widget> focusable)
    {
        if (!widget.Visible || !widget.Enabled)
            return;

        if (widget.IsFocusable)
        {
            focusable.Add(widget);
        }

        var focusCollection = widget.Children.Where(widget => widget.Visible && widget.Enabled)
            .SelectMany(GetAllFocusableWidgets);

        focusable.AddRange(focusCollection);
    }

    IEnumerable<Widget> GetAllFocusableWidgets(Widget root)
    {
        yield return root;
        foreach (var child in root.Children)
        {
            var focusable = GetAllFocusableWidgets(child).Where(x => x.IsFocusable);

            foreach (var descendant in focusable)
            {
                yield return descendant;
            }
        }
    }
}
