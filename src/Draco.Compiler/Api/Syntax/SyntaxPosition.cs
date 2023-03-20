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
