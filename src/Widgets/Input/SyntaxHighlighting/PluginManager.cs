namespace Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;

/// <summary>
/// Manages syntax rule and theme providers with lazy loading and caching support.
/// </summary>
public class PluginManager
{
    private static PluginManager? _instance;
    private readonly Dictionary<string, ISyntaxRuleProvider> _ruleProviders = new();
    private readonly Dictionary<string, IThemeProvider> _themeProviders = new();
    private readonly Dictionary<string, SyntaxRules> _ruleCache = new();
    private readonly Dictionary<string, SyntaxTheme> _themeCache = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the singleton instance of PluginManager.
    /// </summary>
    public static PluginManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PluginManager();
                _instance.InitializeBuiltInProviders();
            }
            return _instance;
        }
    }

    private PluginManager()
    {
    }

    private void InitializeBuiltInProviders()
    {
        var builtInRuleProvider = new BuiltInSyntaxRuleProvider();
        var builtInThemeProvider = new BuiltInThemeProvider();

        RegisterRuleProvider(builtInRuleProvider);
        RegisterThemeProvider(builtInThemeProvider);
    }

    /// <summary>
    /// Registers a syntax rule provider.
    /// </summary>
    /// <param name="provider">The provider to register.</param>
    public void RegisterRuleProvider(ISyntaxRuleProvider provider)
    {
        lock (_lock)
        {
            foreach (var language in provider.GetSupportedLanguages())
            {
                _ruleProviders[language] = provider;
            }
        }
    }

    /// <summary>
    /// Registers a theme provider.
    /// </summary>
    /// <param name="provider">The provider to register.</param>
    public void RegisterThemeProvider(IThemeProvider provider)
    {
        lock (_lock)
        {
            foreach (var theme in provider.GetSupportedThemes())
            {
                _themeProviders[theme] = provider;
            }
        }
    }

    /// <summary>
    /// Gets the syntax rules for the specified language.
    /// </summary>
    /// <param name="language">The language identifier.</param>
    /// <returns>The syntax rules, or null if not found.</returns>
    public SyntaxRules? GetRules(string language)
    {
        if (string.IsNullOrEmpty(language))
            return null;

        lock (_lock)
        {
            if (_ruleCache.TryGetValue(language, out var cached))
            {
                return cached;
            }

            if (_ruleProviders.TryGetValue(language, out var provider))
            {
                var rules = provider.GetRules(language);
                if (rules != null)
                {
                    _ruleCache[language] = rules;
                    return rules;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the theme with the specified name.
    /// </summary>
    /// <param name="name">The theme name.</param>
    /// <returns>The theme, or the default theme if not found.</returns>
    public SyntaxTheme GetTheme(string name)
    {
        if (string.IsNullOrEmpty(name))
            return GetDefaultTheme();

        lock (_lock)
        {
            if (_themeCache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            if (_themeProviders.TryGetValue(name, out var provider))
            {
                var theme = provider.GetTheme(name);
                if (theme != null)
                {
                    _themeCache[name] = theme;
                    return theme;
                }
            }
        }

        return GetDefaultTheme();
    }

    /// <summary>
    /// Gets the default theme.
    /// </summary>
    /// <returns>The default theme.</returns>
    public SyntaxTheme GetDefaultTheme()
    {
        lock (_lock)
        {
            if (_themeProviders.TryGetValue("default", out var provider))
            {
                var theme = provider.GetDefaultTheme();
                if (theme != null)
                {
                    if (!_themeCache.ContainsKey("default"))
                    {
                        _themeCache["default"] = theme;
                    }
                    return theme;
                }
            }
        }

        return new SyntaxTheme
        {
            Name = "default",
            DisplayName = "Default",
            Colors = new Dictionary<TokenType, System.Drawing.Color>()
        };
    }

    /// <summary>
    /// Gets all supported languages from all registered providers.
    /// </summary>
    /// <returns>A collection of supported language identifiers.</returns>
    public IEnumerable<string> GetSupportedLanguages()
    {
        lock (_lock)
        {
            return _ruleProviders.Keys.Distinct().ToList();
        }
    }

    /// <summary>
    /// Gets all supported themes from all registered providers.
    /// </summary>
    /// <returns>A collection of supported theme names.</returns>
    public IEnumerable<string> GetSupportedThemes()
    {
        lock (_lock)
        {
            return _themeProviders.Keys.Distinct().ToList();
        }
    }

    /// <summary>
    /// Clears the rule cache.
    /// </summary>
    public void ClearRuleCache()
    {
        lock (_lock)
        {
            _ruleCache.Clear();
        }
    }

    /// <summary>
    /// Clears the theme cache.
    /// </summary>
    public void ClearThemeCache()
    {
        lock (_lock)
        {
            _themeCache.Clear();
        }
    }
}

