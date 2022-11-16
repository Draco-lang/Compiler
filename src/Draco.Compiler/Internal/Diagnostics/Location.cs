using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Describes what relative positioning is relative to.
/// </summary>
internal enum RelativeOffset
{
    /// <summary>
    /// An invalid offset.
    /// </summary>
    Invalid,

    /// <summary>
    /// Relative to the end of the last element.
    /// </summary>
    EndOfLastElement,

    /// <summary>
    /// Relative to the current element.
    /// </summary>
    CurrentElement,
}

/// <summary>
/// Represents relative range in the source code.
/// </summary>
/// <param name="RelativeTo">Describes what the range is relative to.</param>
/// <param name="Offset">The offset from the element this relates to in characters.</param>
/// <param name="Width">The width of the range in characters.</param>
internal readonly record struct RelativeRange(RelativeOffset RelativeTo, int Offset, int Width)
{
    /// <summary>
    /// Represents no range.
    /// </summary>
    public static readonly RelativeRange None = new(RelativeOffset.Invalid, 0, 0);
}

// NOTE: We'll need to shove the file reference in here

/// <summary>
/// Represents a location in source code.
/// </summary>
/// <param name="Range">The relative range of the location.</param>
internal readonly record struct Location(RelativeRange Range)
{
    /// <summary>
    /// Represents no location.
    /// </summary>
    public static readonly Location None = new();
}
