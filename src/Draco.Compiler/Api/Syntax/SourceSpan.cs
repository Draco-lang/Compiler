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
}
