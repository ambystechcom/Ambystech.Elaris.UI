namespace Ambystech.Elaris.UI.CodeEditor.Plugins.Models;

/// <summary>
/// Data transfer object for deserializing syntax rule JSON files.
/// </summary>
public class SyntaxRuleFile
{
    /// <summary>
    /// Gets or sets the language identifier.
    /// </summary>
    public string Language { get; set; } = "";

    /// <summary>
    /// Gets or sets the list of keywords.
    /// </summary>
    public string[] Keywords { get; set; } = [];

    /// <summary>
    /// Gets or sets the string delimiters.
    /// </summary>
    public string[] StringDelimiters { get; set; } = [];

    /// <summary>
    /// Gets or sets the comment regex patterns.
    /// </summary>
    public string[] CommentPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the number regex pattern.
    /// </summary>
    public string NumberPattern { get; set; } = "";

    /// <summary>
    /// Gets or sets the bracket pairs (opening -> closing).
    /// </summary>
    public Dictionary<char, char> BracketPairs { get; set; } = new();
}

