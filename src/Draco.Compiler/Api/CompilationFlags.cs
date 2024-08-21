using System;

namespace Draco.Compiler.Api;

/// <summary>
/// Special settings flags for the compiler.
/// </summary>
[Flags]
public enum CompilationFlags
{
    /// <summary>
    /// No special settings.
    /// </summary>
    None = 0,

    /// <summary>
    /// All defined symbols will be public in the compilation.
    ///
    /// This can be used by things like the REPL to omit visibility.
    /// </summary>
    ImplicitPublicSymbols = 1 << 0,
}
