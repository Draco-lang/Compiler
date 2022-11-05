using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Diagnostics;

// NOTE: We'll need to shove the file reference in here

/// <summary>
/// Represents a location in source code.
/// </summary>
/// <param name="Offset">The offset from the position this is attached to.</param>
internal readonly record struct Location(int Offset)
{
    /// <summary>
    /// Represents no location.
    /// </summary>
    public static readonly Location None = new(-1);
}
