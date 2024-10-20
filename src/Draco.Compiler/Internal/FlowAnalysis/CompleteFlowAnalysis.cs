using System.Collections.Generic;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.FlowAnalysis.Domains;
using Draco.Compiler.Internal.Symbols;
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
    /// <param name="diagnostics">The diagnostics to report errors to.</param>
    public static void AnalyzeFunction(SourceFunctionSymbol symbol, DiagnosticBag diagnostics)
    {
        var analysis = CreateAnalysis(symbol.Body);
        var flowAnalysis = new CompleteFlowAnalysis(diagnostics, analysis);
        symbol.Body.Accept(flowAnalysis);
    }

    /// <summary>
    /// Analyzes a global value.
    /// </summary>
    /// <param name="symbol">The symbol to analyze.</param>
    /// <param name="diagnostics">The diagnostics to report errors to.</param>
    public static void AnalyzeValue(SourceGlobalSymbol symbol, DiagnosticBag diagnostics)
    {
        if (symbol.Value is null) return;

        var analysis = CreateAnalysis(symbol.Value);
        var flowAnalysis = new CompleteFlowAnalysis(diagnostics, analysis);
        symbol.Value.Accept(flowAnalysis);
    }

    private static DataFlowAnalysis<(ReturnState, Dictionary<LocalSymbol, AssignementState>)> CreateAnalysis(BoundNode node)
    {
        var cfg = ControlFlowGraphBuilder.Build(node);
        var domain = BuildForwardDomain(node);
        return DataFlowAnalysis.Create(cfg, domain);
    }

    private static TupleDomain<ReturnState, Dictionary<LocalSymbol, AssignementState>> BuildForwardDomain(BoundNode node)
    {
        var locals = BoundTreeCollector.CollectLocals(node);
        var returnAnalysisDomain = new ReturnsOnAllPathsDomain();
        var assignmentAnalysisDomain = new DefiniteAssignmentDomain(locals);
        return new(returnAnalysisDomain, assignmentAnalysisDomain);
    }

    private readonly DiagnosticBag diagnostics;
    private readonly DataFlowAnalysis<(ReturnState, Dictionary<LocalSymbol, AssignementState>)> analysis;

    private CompleteFlowAnalysis(
        DiagnosticBag diagnostics,
        DataFlowAnalysis<(ReturnState, Dictionary<LocalSymbol, AssignementState>)> analysis)
    {
        this.diagnostics = diagnostics;
        this.analysis = analysis;
    }

    public override void VisitLocalExpression(BoundLocalExpression node)
    {
        base.VisitLocalExpression(node);

        // TODO
    }

    public override void VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        base.VisitAssignmentExpression(node);

        // TODO
    }
}
