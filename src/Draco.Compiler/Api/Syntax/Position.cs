using System;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents position in a source text.
/// </summary>
/// <param name="Line">The 0-based line number.</param>
/// <param name="Column">The 0-based column number.</param>
public readonly record struct Position(int Line, int Column) : IComparable<Position>
{
    public int CompareTo(Position other)
    {
        var lineCmp = this.Line.CompareTo(other.Line);
        return lineCmp != 0 ? lineCmp : this.Column.CompareTo(other.Column);
    }

    public static bool operator <(Position left, Position right) => left.CompareTo(right) < 0;
    public static bool operator <=(Position left, Position right) => left.CompareTo(right) <= 0;
    public static bool operator >(Position left, Position right) => left.CompareTo(right) > 0;
    public static bool operator >=(Position left, Position right) => left.CompareTo(right) >= 0;
}

/// <summary>
/// Represents a range in a source text.
/// </summary>
/// <param name="Start">The inclusive start of the range.</param>
/// <param name="End">The exclusive end of the range.</param>
public readonly record struct Range(Position Start, Position End)
{
    /// <summary>
    /// Constructs a range from a starting position and length.
    /// </summary>
    /// <param name="start">The inclusive start of the range.</param>
    /// <param name="length">The horizontal length of the range.</param>
    public Range(Position start, int length)
        : this(start, new Position(Line: start.Line, Column: start.Column + length))
    {
    }

    /// <summary>
    /// Checks if this range contains the given position.
    /// </summary>
    /// <param name="position">The position to check for containment.</param>
    /// <returns>True, if <paramref name="position"/> is contained, false otherwise.</returns>
    public bool Contains(Position position) => this.Start <= position && position < this.End;
}
