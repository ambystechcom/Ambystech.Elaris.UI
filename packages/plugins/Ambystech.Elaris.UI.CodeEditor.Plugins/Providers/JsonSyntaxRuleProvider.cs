using System.Text.Json;
using Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;
using Ambystech.Elaris.UI.CodeEditor.Plugins.Models;

namespace Ambystech.Elaris.UI.CodeEditor.Plugins.Providers;

/// <summary>
/// Provider that loads syntax rules from JSON files.
/// </summary>
public class JsonSyntaxRuleProvider : ISyntaxRuleProvider
{
    private readonly string _pluginDirectory;
    private readonly Dictionary<string, SyntaxRules> _cache = new();
    private readonly object _lock = new();
    private bool _initialized = false;

    /// <summary>
    /// Initializes a new instance of JsonSyntaxRuleProvider.
    /// </summary>
    /// <param name="pluginDirectory">The directory containing syntax rule JSON files.</param>
    public JsonSyntaxRuleProvider(string pluginDirectory)
    {
        _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
    }

    /// <summary>
    /// Loads syntax rules from the plugin directory.
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
                    var ruleFile = JsonSerializer.Deserialize<SyntaxRuleFile>(json);

                    if (ruleFile != null && !string.IsNullOrEmpty(ruleFile.Language))
                    {
                        var rules = new SyntaxRules
                        {
                            Keywords = ruleFile.Keywords,
                            StringDelimiters = ruleFile.StringDelimiters,
                            CommentPatterns = ruleFile.CommentPatterns,
                            NumberPattern = ruleFile.NumberPattern,
                            BracketPairs = ruleFile.BracketPairs
                        };

                        _cache[ruleFile.Language] = rules;
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

    public SyntaxRules? GetRules(string language)
    {
        if (string.IsNullOrEmpty(language))
            return null;

        LoadFromDirectory();

        lock (_lock)
        {
            return _cache.TryGetValue(language, out var rules) ? rules : null;
        }
    }

    public IEnumerable<string> GetSupportedLanguages()
    {
        LoadFromDirectory();

        lock (_lock)
        {
            return _cache.Keys.ToList();
        }
    }
}

