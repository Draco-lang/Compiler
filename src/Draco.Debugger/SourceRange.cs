namespace Draco.Debugger;

/// <summary>
/// Represents a range of source code.
/// </summary>
/// <param name="Start">The inclusive start position.</param>
/// <param name="End">The exclusive end position.</param>
public readonly record struct SourceRange(SourcePosition Start, SourcePosition End)
{
    public SourceRange(int startLine, int startColumn, int endLine, int endColumn)
        : this(
              new SourcePosition(Line: startLine, Column: startColumn),
              new SourcePosition(Line: endLine, Column: endColumn))
    {
    }

    /// <summary>
    /// Checks if the given position is within this range.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>True, if this range contains <paramref name="position"/>.</returns>
    public bool Contains(SourcePosition position) => this.Start <= position && position < this.End;
}
