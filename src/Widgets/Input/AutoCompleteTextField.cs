using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Input;

/// <summary>
/// A text field with autocomplete suggestions displayed in a popup ListBox.
/// </summary>
public class AutoCompleteTextField : TextField
{
    private readonly ListBox _listBox;
    private CancellationTokenSource? _debounceCts;
    private bool _isPopupVisible = false;

    /// <summary>
    /// Delegate for providing suggestions based on the current text input.
    /// </summary>
    public Func<string, Task<List<string>>>? SuggestionsProvider { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of suggestions to display.
    /// </summary>
    public int MaxSuggestions { get; set; } = 10;

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds before querying suggestions.
    /// </summary>
    public int DebounceDelay { get; set; } = 200;

    /// <summary>
    /// Gets or sets whether to show the popup above the text field.
    /// This is auto-calculated based on available screen space during rendering.
    /// </summary>
    public bool ShowPopupAbove { get; set; } = false;

    /// <summary>
    /// Gets or sets the background color for suggestions.
    /// </summary>
    public Color SuggestionBackgroundColor
    {
        get => _listBox.BackgroundColor;
        set => _listBox.BackgroundColor = value;
    }

    /// <summary>
    /// Gets or sets the foreground color for suggestions.
    /// </summary>
    public Color SuggestionForegroundColor
    {
        get => _listBox.ForegroundColor;
        set => _listBox.ForegroundColor = value;
    }

    /// <summary>
    /// Gets or sets the highlight color for the selected suggestion.
    /// </summary>
    public Color SelectionHighlightColor
    {
        get => _listBox.SelectionBackgroundColor;
        set => _listBox.SelectionBackgroundColor = value;
    }

    /// <summary>
    /// Event raised when a suggestion is selected.
    /// </summary>
    public event Action<string>? SuggestionSelected;

    /// <summary>
    /// Event raised when the popup opens.
    /// </summary>
    public event Action? PopupOpened;

    /// <summary>
    /// Event raised when the popup closes.
    /// </summary>
    public event Action? PopupClosed;

    public AutoCompleteTextField()
    {
        _listBox = new ListBox
        {
            Visible = false,
            ZIndex = 1000, // Ensure popup renders on top
            BackgroundColor = ColorHelper.FromRgb(30, 30, 30),
            ForegroundColor = Color.White,
            SelectionBackgroundColor = ColorHelper.FromRgb(50, 100, 150),
            SelectionForegroundColor = Color.White
        };

        TextChanged += OnTextChangedInternal;
    }

    public AutoCompleteTextField(string placeholder) : base(placeholder)
    {
        _listBox = new ListBox
        {
            Visible = false,
            ZIndex = 1000,
            BackgroundColor = ColorHelper.FromRgb(30, 30, 30),
            ForegroundColor = Color.White,
            SelectionBackgroundColor = ColorHelper.FromRgb(50, 100, 150),
            SelectionForegroundColor = Color.White
        };

        TextChanged += OnTextChangedInternal;
    }

    /// <summary>
    /// Shows the suggestions popup with the provided list of suggestions.
    /// </summary>
    public void ShowSuggestions(List<string> suggestions)
    {
        if (suggestions == null || suggestions.Count == 0)
        {
            HideSuggestions();
            return;
        }

        _listBox.ClearItems();
        int count = Math.Min(suggestions.Count, MaxSuggestions);
        for (int i = 0; i < count; i++)
        {
            _listBox.AddItem(suggestions[i]);
        }

        if (!_isPopupVisible)
        {
            _isPopupVisible = true;
            _listBox.Visible = true;
            _listBox.SelectedIndex = 0; // Select first item
            PopupOpened?.Invoke();
        }
    }

    /// <summary>
    /// Hides the suggestions popup.
    /// </summary>
    public void HideSuggestions()
    {
        if (_isPopupVisible)
        {
            _isPopupVisible = false;
            _listBox.Visible = false;
            _listBox.ClearItems();
            PopupClosed?.Invoke();
        }
    }

    /// <summary>
    /// Manually refreshes suggestions based on the current text.
    /// </summary>
    public async void RefreshSuggestions()
    {
        if (SuggestionsProvider == null || string.IsNullOrWhiteSpace(Text))
        {
            HideSuggestions();
            return;
        }

        await LoadSuggestionsAsync(Text, StartNewQuery());
    }

    /// <summary>
    /// Cancels the previous query (debounce or provider call) and returns a token for a new one.
    /// </summary>
    private CancellationToken StartNewQuery()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        return _debounceCts.Token;
    }

    private void OnTextChangedInternal(string newText)
    {
        var token = StartNewQuery();

        if (string.IsNullOrWhiteSpace(newText) || SuggestionsProvider == null)
        {
            HideSuggestions();
            return;
        }

        _ = DebounceAndLoadAsync(newText, token);
    }

    /// <remarks>
    /// Runs on the caller's <see cref="SynchronizationContext"/>. Inside <see cref="Application.Run"/>
    /// that is the UI thread, so the suggestions are applied there, not on a timer thread.
    /// </remarks>
    private async Task DebounceAndLoadAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(DebounceDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await LoadSuggestionsAsync(text, cancellationToken);
    }

    private async Task LoadSuggestionsAsync(string text, CancellationToken cancellationToken)
    {
        if (SuggestionsProvider == null)
            return;

        try
        {
            var suggestions = await SuggestionsProvider(text);

            // A newer query replaced this one while the provider was running.
            if (!cancellationToken.IsCancellationRequested)
            {
                ShowSuggestions(suggestions);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            if (!cancellationToken.IsCancellationRequested)
                HideSuggestions();
        }
    }

    protected override void OnRender(Screen screen)
    {
        base.OnRender(screen);

        if (_isPopupVisible && _listBox.Visible && _listBox.Items.Count > 0)
        {
            int popupHeight = Math.Min(_listBox.Items.Count, MaxSuggestions);
            int popupWidth = Width;

            int spaceBelow = screen.Height - (Y + 1);
            int spaceAbove = Y;

            bool showAbove = spaceBelow < popupHeight && spaceAbove > spaceBelow;
            ShowPopupAbove = showAbove;

            int popupX = X;
            int popupY;

            if (showAbove)
            {
                popupY = Math.Max(0, Y - popupHeight);
                popupHeight = Math.Min(popupHeight, Y);
            }
            else
            {
                popupY = Y + 1;
                popupHeight = Math.Min(popupHeight, spaceBelow);
            }

            if (popupX + popupWidth > screen.Width)
            {
                popupWidth = screen.Width - popupX;
            }

            _listBox.Bounds = new Rectangle(popupX, popupY, popupWidth, popupHeight);

            // The popup may extend past the container the field is in.
            using (screen.PushOverlay())
            {
                _listBox.Render(screen);
            }
        }
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        if (_isPopupVisible && _listBox.Visible)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (_listBox.SelectedIndex > 0)
                    {
                        _listBox.SelectedIndex--;
                    }
                    return true;

                case ConsoleKey.DownArrow:
                    if (_listBox.SelectedIndex < _listBox.Items.Count - 1)
                    {
                        _listBox.SelectedIndex++;
                    }
                    return true;

                case ConsoleKey.Enter:
                case ConsoleKey.Tab:
                    AcceptSuggestion();
                    return true;

                case ConsoleKey.Escape:
                    HideSuggestions();
                    return true;
            }
        }

        return base.OnKeyPress(key);
    }

    private void AcceptSuggestion()
    {
        if (_listBox.SelectedItem != null)
        {
            Text = _listBox.SelectedItem;

            typeof(TextField).GetField("_cursorPosition",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(this, Text.Length);

            SuggestionSelected?.Invoke(_listBox.SelectedItem);

            HideSuggestions();
        }
    }

    /// <summary>
    /// Disposes resources used by this widget.
    /// </summary>
    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
    }
}
