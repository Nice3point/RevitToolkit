namespace Nice3point.Revit.Toolkit.SourceGenerators;

/// <summary>
///     Interface for fluent code generation with automatic formatting and indentation.
/// </summary>
internal interface ICodeWriter : IDisposable
{
    /// <summary>
    ///     Gets the current indentation level.
    /// </summary>
    int IndentLevel { get; }

    /// <summary>
    ///     Appends a line of text with proper indentation.
    ///     When called without arguments or with an empty string, emits a blank line.
    /// </summary>
    ICodeWriter AppendLine(string line = "");

    /// <summary>
    ///     Appends text without adding a new line.
    /// </summary>
    ICodeWriter Append(string text);

    /// <summary>
    ///     Appends multiple lines of code.
    /// </summary>
    ICodeWriter AppendLines(IEnumerable<string> lines);

    /// <summary>
    ///     Appends a code block with automatic braces and indentation.
    /// </summary>
    ICodeWriter AppendBlock(string header, Action<ICodeWriter> body);

    /// <summary>
    ///     Increases the indentation level.
    /// </summary>
    ICodeWriter Indent();

    /// <summary>
    ///     Decreases the indentation level.
    /// </summary>
    ICodeWriter Unindent();

    /// <summary>
    ///     Begins a code block with automatic formatting, handling opening brace and indentation.
    ///     Returns an IDisposable that will unindent and append closing brace when disposed.
    /// </summary>
    IDisposable BeginBlock(string leadingText = "");

    /// <summary>
    ///     Conditionally appends a line.
    /// </summary>
    ICodeWriter AppendLineIf(bool condition, string line);

    /// <summary>
    ///     Gets the generated code as a string.
    /// </summary>
    string ToString();

    /// <summary>
    ///     Sets the initial indentation level.
    /// </summary>
    ICodeWriter SetIndentLevel(int level);
}
