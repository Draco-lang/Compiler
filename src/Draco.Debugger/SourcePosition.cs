using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger;

/// <summary>
/// Represents a position in source code.
/// </summary>
/// <param name="Line">The 0-based line index.</param>
/// <param name="Column">The 0-based column index.</param>
public readonly record struct SourcePosition(int Line, int Column) : IComparable<SourcePosition>
{
    public int CompareTo(SourcePosition other)
    {
        var cmp = this.Line.CompareTo(other.Line);
        return cmp == 0
            ? this.Column.CompareTo(other.Column)
            : cmp;
    }

    public static bool operator <(SourcePosition left, SourcePosition right) => left.CompareTo(right) < 0;
    public static bool operator <=(SourcePosition left, SourcePosition right) => left.CompareTo(right) <= 0;
    public static bool operator >(SourcePosition left, SourcePosition right) => left.CompareTo(right) > 0;
    public static bool operator >=(SourcePosition left, SourcePosition right) => left.CompareTo(right) >= 0;
}
