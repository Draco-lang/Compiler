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
    ConstantEvaluation = 4,
    FlowAnalysis = 5,
    Codegen = 6,
}
