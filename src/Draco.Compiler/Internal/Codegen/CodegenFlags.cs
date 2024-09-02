using System;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Flags for code generation.
/// </summary>
[Flags]
internal enum CodegenFlags
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,

    /// <summary>
    /// Emit PDB information.
    /// </summary>
    EmitPdb = (1 << 0),

    /// <summary>
    /// Attempt to recover stack-structure from register-based code, potentially causing less instructions to be emitted.
    /// </summary>
    Stackify = (1 << 1),

    /// <summary>
    /// Redirect all source references to the root module.
    /// </summary>
    RedirectHandlesToRoot = (1 << 2),
}
