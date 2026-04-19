namespace Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;

/// <summary>
/// Defines syntax highlighting rules for a programming language.
/// </summary>
public class SyntaxRules
{
    /// <summary>
    /// Gets or sets the list of keywords for the language.
    /// </summary>
    public string[] Keywords { get; set; } = [];

    /// <summary>
    /// Gets or sets the string delimiters (e.g., ", ', `).
    /// </summary>
    public string[] StringDelimiters { get; set; } = [];

    /// <summary>
    /// Gets or sets the regex patterns for comments.
    /// </summary>
    public string[] CommentPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the regex pattern for numbers.
    /// </summary>
    public string NumberPattern { get; set; } = "";

    /// <summary>
    /// Gets or sets the bracket pairs (opening -> closing).
    /// </summary>
    public Dictionary<char, char> BracketPairs { get; set; } = new();
}

