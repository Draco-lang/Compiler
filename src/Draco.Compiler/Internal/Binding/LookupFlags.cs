using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Flags for looking up symbols.
/// </summary>
[Flags]
internal enum LookupFlags
{
    /// <summary>
    /// Default behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Disallow looking up locals.
    /// </summary>
    DisallowLocals = 1 << 0,
}
