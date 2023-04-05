namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Possible categories of diagnostic.
/// </summary>
internal enum DiagnosticCategory
{
    InternalCompiler = 0,
    Syntax = 1,
    SymbolResolution = 2,
    TypeChecking = 3,
    FlowAnalysis = 4,
    Codegen = 5,
}
