namespace Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;

/// <summary>
/// Built-in syntax rule provider that supplies rules for common programming languages.
/// </summary>
internal class BuiltInSyntaxRuleProvider : ISyntaxRuleProvider
{
    private static readonly Dictionary<string, SyntaxRules> Rules = new()
    {
        ["csharp"] = new SyntaxRules
        {
            Keywords = new[] { "using", "namespace", "class", "public", "private", "protected", "internal", "static", "void", "int", "string", "bool", "var", "if", "else", "for", "foreach", "while", "return", "new", "this", "base", "true", "false", "null", "async", "await", "task" },
            StringDelimiters = new[] { "\"", "'", "@\"" },
            CommentPatterns = new[] { @"//.*", @"/\*.*?\*/" },
            NumberPattern = @"\b\d+\.?\d*\b",
            BracketPairs = new Dictionary<char, char> { { '(', ')' }, { '[', ']' }, { '{', '}' } }
        },
        ["javascript"] = new SyntaxRules
        {
            Keywords = new[] { "function", "var", "let", "const", "if", "else", "for", "while", "return", "async", "await", "class", "extends", "import", "export", "default", "true", "false", "null", "undefined" },
            StringDelimiters = new[] { "\"", "'", "`" },
            CommentPatterns = new[] { @"//.*", @"/\*.*?\*/" },
            NumberPattern = @"\b\d+\.?\d*\b",
            BracketPairs = new Dictionary<char, char> { { '(', ')' }, { '[', ']' }, { '{', '}' } }
        },
        ["python"] = new SyntaxRules
        {
            Keywords = new[] { "def", "class", "if", "elif", "else", "for", "while", "return", "import", "from", "as", "try", "except", "finally", "with", "True", "False", "None", "and", "or", "not", "in", "is" },
            StringDelimiters = new[] { "\"", "'", "\"\"\"", "'''" },
            CommentPatterns = new[] { @"#.*" },
            NumberPattern = @"\b\d+\.?\d*\b",
            BracketPairs = new Dictionary<char, char> { { '(', ')' }, { '[', ']' }, { '{', '}' } }
        }
    };

    public SyntaxRules? GetRules(string language)
    {
        return Rules.TryGetValue(language, out var rules) ? rules : null;
    }

    public IEnumerable<string> GetSupportedLanguages()
    {
        return Rules.Keys;
    }
}

