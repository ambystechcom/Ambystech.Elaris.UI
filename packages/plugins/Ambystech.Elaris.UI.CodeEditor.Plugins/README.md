# Ambystech.Elaris.UI.CodeEditor.Plugins

File-based syntax highlighting plugins for Elaris.UI CodeEditor.

## Installation

```bash
dotnet add package Ambystech.Elaris.UI.CodeEditor.Plugins
```

## Usage

### Initializing Plugins

```csharp
using Ambystech.Elaris.UI.CodeEditor.Plugins;

// Initialize plugins from directories
PluginInitializer.Initialize(
    syntaxRulesDirectory: "path/to/syntax/rules",
    themesDirectory: "path/to/themes"
);
```

### Creating Syntax Rule Files

Create JSON files for each language you want to support:

**Example: `rust.json`**
```json
{
  "language": "rust",
  "keywords": ["fn", "let", "mut", "pub", "struct", "enum"],
  "stringDelimiters": ["\"", "r#\""],
  "commentPatterns": ["//.*", "/\\*.*?\\*/"],
  "numberPattern": "\\b\\d+(\\.\\d+)?\\b",
  "bracketPairs": {
    "(": ")",
    "[": "]",
    "{": "}"
  }
}
```

### Creating Theme Files

Create JSON files for each theme:

**Example: `monokai.json`**
```json
{
  "name": "monokai",
  "displayName": "Monokai",
  "colors": {
    "Keyword": { "r": 249, "g": 38, "b": 114 },
    "String": { "r": 230, "g": 219, "b": 116 },
    "Comment": { "r": 117, "g": 113, "b": 94 },
    "Number": { "r": 174, "g": 129, "b": 255 },
    "Operator": { "r": 248, "g": 248, "b": 242 },
    "Identifier": { "r": 248, "g": 248, "b": 242 }
  }
}
```

### Using Plugins in CodeEditor

```csharp
var codeEditor = new CodeEditor
{
    Language = "rust",  // Uses plugin-provided rules
    Theme = "monokai"   // Uses plugin-provided theme
};
```

## Plugin Directory Structure

```
plugins/
├── syntax/
│   ├── rust.json
│   ├── go.json
│   └── ...
└── themes/
    ├── monokai.json
    ├── solarized.json
    └── ...
```

## Features

- **Lazy Loading**: Plugins are loaded only when needed
- **Caching**: Loaded rules and themes are cached in memory
- **Fallback**: Falls back to built-in providers if plugin not found
- **Thread-Safe**: Safe for concurrent access

## Token Types

- `Keyword`: Language keywords
- `String`: String literals
- `Comment`: Comments
- `Number`: Numeric literals
- `Operator`: Operators and punctuation
- `Identifier`: Identifiers and variable names

## License

MIT

