using System;

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
    None = 0 << 0,

    /// <summary>
    /// Disallow looking up locals.
    /// </summary>
    DisallowLocals = 1 << 0,

    /// <summary>
    /// Allow looking up globals.
    /// </summary>
    AllowGlobals = 1 << 1,
}
