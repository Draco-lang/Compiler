using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Performs a complete flow analysis on a bound tree, reporting all errors.
/// </summary>
internal sealed class CompleteFlowAnalysis : BoundTreeVisitor
{
    /// <summary>
    /// Analyzes a function body.
    /// </summary>
    /// <param name="symbol">The symbol to analyze.</param>
    public static void AnalyzeFunction(SourceFunctionSymbol symbol)
    {
    }

    /// <summary>
    /// Analyzes a global value.
    /// </summary>
    /// <param name="symbol">The symbol to analyze.</param>
    public static void AnalyzeValue(SourceGlobalSymbol symbol)
    {
    }

    private CompleteFlowAnalysis()
    {
    }
}
