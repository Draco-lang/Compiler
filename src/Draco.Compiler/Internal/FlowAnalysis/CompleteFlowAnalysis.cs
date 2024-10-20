using System.Collections.Generic;
using Draco.Compiler.Api.Diagnostics;
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

        // We need to check the exit state, if it's returning on all paths
        var (returnState, _) = analysis.GetExit(symbol.Body);
        if (returnState != ReturnState.Returns)
        {
            diagnostics.Add(Diagnostic.Create(
                template: FlowAnalysisErrors.DoesNotReturn,
                location: symbol.DeclaringSyntax.Name.Location,
                formatArgs: symbol.Name));
        }
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
    private readonly DataFlowAnalysis<(ReturnState Return, Dictionary<LocalSymbol, AssignementState> LocalAssignment)> analysis;

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

        var (@return, localAssignment) = this.analysis.GetEntry(node);
        if (!localAssignment.TryGetValue(node.Local, out var state) || state != AssignementState.DefinitelyAssigned)
        {
            // Referencing an unassigned local, error
            this.diagnostics.Add(Diagnostic.Create(
                template: FlowAnalysisErrors.VariableUsedBeforeInit,
                location: node.Syntax?.Location,
                formatArgs: node.Local.Name));
        }
    }

    public override void VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        base.VisitAssignmentExpression(node);
        if (node.Left is not BoundLocalLvalue localLvalue) return;

        var (@return, localAssignment) = this.analysis.GetEntry(node);

        // First we check, if we have a compound assignment
        if (node.CompoundOperator is not null)
        {
            // In this case, the left side is also a read first, needs to be assigned
            if (!localAssignment.TryGetValue(localLvalue.Local, out var state) || state != AssignementState.DefinitelyAssigned)
            {
                // Referencing an unassigned local, error
                this.diagnostics.Add(Diagnostic.Create(
                    template: FlowAnalysisErrors.VariableUsedBeforeInit,
                    location: node.Left.Syntax?.Location,
                    formatArgs: localLvalue.Local.Name));
            }
        }

        // If we have an immutable local, we need to check, if it's been assigned before
        if (!localLvalue.Local.IsMutable)
        {
            if (localAssignment.TryGetValue(localLvalue.Local, out var state) && state != AssignementState.DefinitelyUnassigned)
            {
                // We are trying to assign multiple times to an immutable local
                this.diagnostics.Add(Diagnostic.Create(
                    template: FlowAnalysisErrors.ImmutableVariableCanNotBeAssignedTo,
                    location: node.Syntax?.Location,
                    formatArgs: localLvalue.Local.Name));
            }
        }
    }
}
