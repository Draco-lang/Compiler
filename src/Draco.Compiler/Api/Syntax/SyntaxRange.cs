namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents a range in a source text.
/// </summary>
/// <param name="Start">The inclusive start of the range.</param>
/// <param name="End">The exclusive end of the range.</param>
public readonly record struct SyntaxRange(SyntaxPosition Start, SyntaxPosition End)
{
    public static readonly SyntaxRange Empty = default;

    /// <summary>
    /// Constructs a range from a starting position and length.
    /// </summary>
    /// <param name="start">The inclusive start of the range.</param>
    /// <param name="length">The horizontal length of the range.</param>
    public SyntaxRange(SyntaxPosition start, int length)
        : this(start, new SyntaxPosition(Line: start.Line, Column: start.Column + length))
    {
    }

    /// <summary>
    /// Checks if this range contains the given position.
    /// </summary>
    /// <param name="position">The position to check for containment.</param>
    /// <returns>True, if <paramref name="position"/> is contained, false otherwise.</returns>
    public bool Contains(SyntaxPosition position) => this.Start <= position && position < this.End;

    /// <summary>
    /// Checks if this range contains the given range.
    /// </summary>
    /// <param name="range">The range to check for containment.</param>
    /// <returns>True, if <paramref name="range"/> is contained, false otherwise.</returns>
    public bool Contains(SyntaxRange range) => this.Start <= range.Start && this.End >= range.End;

    /// <summary>
    /// Checks if this range is disjunct with the given range.
    /// </summary>
    /// <param name="range">The range to check for disjunction.</param>
    /// <returns>True, if this range and <paramref name="range"/> are disjunct, false otherwise.</returns>
    public bool IsDisjunctWith(SyntaxRange range) => this.End <= range.Start || range.End <= this.Start;

    /// <summary>
    /// Checks if this range intersects with the given range.
    /// </summary>
    /// <param name="range">The range to check for intersection.</param>
    /// <returns>True, if this range and <paramref name="range"/> are intersecting, false otherwise.</returns>
    public bool Intersects(SyntaxRange range) => !this.IsDisjunctWith(range);
}
