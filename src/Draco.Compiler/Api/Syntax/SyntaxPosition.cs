using System;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents position in a source text.
/// </summary>
/// <param name="Line">The 0-based line number.</param>
/// <param name="Column">The 0-based column number.</param>
public readonly record struct SyntaxPosition(int Line, int Column) : IComparable<SyntaxPosition>
{
    /// <summary>
    /// Offsets this position by the given syntax offset.
    /// </summary>
    /// <param name="offset">The syntax offset to offset by.</param>
    /// <returns>The new position, offset by <paramref name="offset"/>.</returns>
    internal SyntaxPosition OffsetBy(SyntaxOffset offset) => offset.Lines == 0
        ? new(Line: this.Line, Column: this.Column + offset.Columns)
        : new(Line: this.Line + offset.Lines, Column: offset.Columns);

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
