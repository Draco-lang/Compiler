using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Represents a syntactical offset in text.
/// </summary>
/// <param name="Lines">The number of lines offset.</param>
/// <param name="Columns">The number of columns offset.</param>
internal readonly record struct SyntaxOffset(int Lines, int Columns)
{
    /// <summary>
    /// Offsets thus syntax offset by another.
    /// </summary>
    /// <param name="offset">The other offset to offset by.</param>
    /// <returns>The new syntax offset, offset by <paramref name="offset"/>.</returns>
    public SyntaxOffset OffsetBy(SyntaxOffset offset) => offset.Lines == 0
        ? new(Lines: this.Lines, Columns: this.Columns + offset.Columns)
        : new(Lines: this.Lines + offset.Lines, Columns: offset.Columns);
}
