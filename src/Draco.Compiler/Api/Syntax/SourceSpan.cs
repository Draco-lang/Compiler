namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents a portion of source text.
/// </summary>
/// <param name="Start">The start index of the text.</param>
/// <param name="Length">The length of the text.</param>
public readonly record struct SourceSpan(int Start, int Length)
{
    /// <summary>
    /// The end index of the text (exclusive).
    /// </summary>
    public int End => this.Start + this.Length;

    /// <summary>
    /// Checks if this span contains the given index.
    /// </summary>
    /// <param name="index">The index to check for containment.</param>
    /// <returns>True, if <paramref name="index"/> is contained, false otherwise.</returns>
    public bool Contains(int index) => this.Start <= index && index < this.End;

    /// <summary>
    /// Checks if this span contains the given span.
    /// </summary>
    /// <param name="span">The span to check for containment.</param>
    /// <returns>True, if <paramref name="span"/> is contained, false otherwise.</returns>
    public bool Contains(SourceSpan span) => this.Start <= span.Start && this.End >= span.End;

    /// <summary>
    /// Checks if this span is disjunct with the given span.
    /// </summary>
    /// <param name="span">The span to check for disjunction.</param>
    /// <returns>True, if this span and <paramref name="span"/> are disjunct, false otherwise.</returns>
    public bool IsDisjunctWith(SourceSpan span) => this.End <= span.Start || span.End <= this.Start;

    /// <summary>
    /// Checks if this span intersects with the given span.
    /// </summary>
    /// <param name="span">The span to check for intersection.</param>
    /// <returns>True, if this span and <paramref name="span"/> are intersecting, false otherwise.</returns>
    public bool Intersects(SourceSpan span) => !this.IsDisjunctWith(span);
}
