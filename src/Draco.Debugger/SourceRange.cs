namespace Draco.Debugger;

/// <summary>
/// Represents a range of source code.
/// </summary>
/// <param name="StartLine">The 0-based start line index.</param>
/// <param name="StartColumn">The 0-based start column index.</param>
/// <param name="EndLine">The 0-based end line index (exclusive).</param>
/// <param name="EndColumn">The 0-based end column index (exclusive).</param>
public readonly record struct SourceRange(
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
