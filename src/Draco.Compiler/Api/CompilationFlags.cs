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

    /// <summary>
    /// The compilation is in scripting mode.
    ///
    /// This generally means that it will only consume a single syntax tree with a single
    /// script entry syntax.
    /// </summary>
    ScriptingMode = 1 << 1,
}
