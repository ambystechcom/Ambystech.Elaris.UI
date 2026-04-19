using System.Drawing;
using System.Text.Json;
using Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;
using Ambystech.Elaris.UI.CodeEditor.Plugins.Models;

namespace Ambystech.Elaris.UI.CodeEditor.Plugins.Providers;

/// <summary>
/// Provider that loads themes from JSON files.
/// </summary>
public class JsonThemeProvider : IThemeProvider
{
    private readonly string _pluginDirectory;
    private readonly Dictionary<string, SyntaxTheme> _cache = new();
    private SyntaxTheme? _defaultTheme;
    private readonly object _lock = new();
    private bool _initialized = false;

    /// <summary>
    /// Initializes a new instance of JsonThemeProvider.
    /// </summary>
    /// <param name="pluginDirectory">The directory containing theme JSON files.</param>
    public JsonThemeProvider(string pluginDirectory)
    {
        _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
    }

    /// <summary>
    /// Loads themes from the plugin directory.
    /// </summary>
    public void LoadFromDirectory()
    {
        if (_initialized)
            return;

        lock (_lock)
        {
            if (_initialized)
                return;

            if (!Directory.Exists(_pluginDirectory))
                return;

            var jsonFiles = Directory.GetFiles(_pluginDirectory, "*.json", SearchOption.TopDirectoryOnly);

            foreach (var file in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var themeFile = JsonSerializer.Deserialize<ThemeFile>(json);

                    if (themeFile != null && !string.IsNullOrEmpty(themeFile.Name))
                    {
                        var colors = new Dictionary<TokenType, Color>();

                        foreach (var kvp in themeFile.Colors)
                        {
                            if (Enum.TryParse<TokenType>(kvp.Key, ignoreCase: true, out var tokenType))
                            {
                                colors[tokenType] = Color.FromArgb(255, kvp.Value.R, kvp.Value.G, kvp.Value.B);
                            }
                        }

                        var theme = new SyntaxTheme
                        {
                            Name = themeFile.Name,
                            DisplayName = themeFile.DisplayName,
                            Colors = colors
                        };

                        _cache[themeFile.Name] = theme;

                        if (themeFile.Name == "default" && _defaultTheme == null)
                        {
                            _defaultTheme = theme;
                        }
                    }
                }
                catch
                {
                    // Ignore invalid JSON files
                }
            }

            _initialized = true;
        }
    }

    public SyntaxTheme? GetTheme(string name)
    {
        if (string.IsNullOrEmpty(name))
            return GetDefaultTheme();

        LoadFromDirectory();

        lock (_lock)
        {
            return _cache.TryGetValue(name, out var theme) ? theme : null;
        }
    }

    public SyntaxTheme GetDefaultTheme()
    {
        LoadFromDirectory();

        lock (_lock)
        {
            if (_defaultTheme != null)
                return _defaultTheme;

            if (_cache.Count > 0)
            {
                _defaultTheme = _cache.Values.First();
                return _defaultTheme;
            }
        }

        return new SyntaxTheme
        {
            Name = "default",
            DisplayName = "Default",
            Colors = new Dictionary<TokenType, Color>()
        };
    }

    public IEnumerable<string> GetSupportedThemes()
    {
        LoadFromDirectory();

        lock (_lock)
        {
            return _cache.Keys.ToList();
        }
    }
}

