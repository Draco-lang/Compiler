using System;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents position in a source text.
/// </summary>
/// <param name="Line">The 0-based line number.</param>
/// <param name="Column">The 0-based column number.</param>
public readonly record struct SyntaxPosition(int Line, int Column) : IComparable<SyntaxPosition>
{
    /// <summary>
    /// Computes the relative position relative to a starting point.
    /// </summary>
    /// <param name="start">The starting point.</param>
    /// <returns>The position relative to <paramref name="start"/>.</returns>
    public SyntaxPosition RelativeTo(SyntaxPosition start) => start.Line == this.Line
        ? new(Line: 0, Column: this.Column - start.Column)
        : new(Line: this.Line - start.Line, Column: this.Column);

    public int CompareTo(SyntaxPosition other)
    {
        var lineCmp = this.Line.CompareTo(other.Line);
        return lineCmp != 0 ? lineCmp : this.Column.CompareTo(other.Column);
    }

    public static bool operator <(SyntaxPosition left, SyntaxPosition right) => left.CompareTo(right) < 0;
    public static bool operator <=(SyntaxPosition left, SyntaxPosition right) => left.CompareTo(right) <= 0;
    public static bool operator >(SyntaxPosition left, SyntaxPosition right) => left.CompareTo(right) > 0;
    public static bool operator >=(SyntaxPosition left, SyntaxPosition right) => left.CompareTo(right) >= 0;
}
