using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents position in a source text.
/// </summary>
/// <param name="Line">The 0-based line number.</param>
/// <param name="Column">The 0-based column number.</param>
public readonly record struct Position(int Line, int Column);

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
}
