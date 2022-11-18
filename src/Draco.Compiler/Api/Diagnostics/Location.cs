using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Diagnostics;

// NOTE: We'll need a file reference here

/// <summary>
/// Represents a location in a source text.
/// </summary>
/// <param name="Range">The range of the represented location.</param>
public readonly record struct Location(Syntax.Range Range)
{
    public static readonly Location None = default;

    // TODO
    public bool IsNone => false;
}
