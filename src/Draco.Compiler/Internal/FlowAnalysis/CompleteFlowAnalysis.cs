using System.Collections;
using System.Collections.Generic;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
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
        var cfg = ControlFlowGraphBuilder.Build(symbol.Body);
        var analysis = CreateAnalysis(diagnostics, symbol.Body, cfg);
        symbol.Body.Accept(analysis);

        // We need to check the exit state, if it's returning on all paths
        if (cfg.Exit is not null)
        {
            var (returnState, _, _) = analysis.analysis.GetExit(cfg.Exit);
            if (returnState != ReturnState.Returns)
            {
                diagnostics.Add(Diagnostic.Create(
                    template: FlowAnalysisErrors.DoesNotReturn,
                    location: symbol.DeclaringSyntax.Name.Location,
                    formatArgs: symbol.Name));
            }
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

        var cfg = ControlFlowGraphBuilder.Build(symbol.Value);
        var analysis = CreateAnalysis(diagnostics, symbol.Value, cfg);
        symbol.Value.Accept(analysis);
    }

    private static CompleteFlowAnalysis CreateAnalysis(
        DiagnosticBag diagnostics,
        BoundNode node,
        IControlFlowGraph cfg)
    {
        var locals = BoundTreeCollector.CollectLocals(node);

        var returnsOnAllPathsDomain = new ReturnsOnAllPathsDomain();
        var definiteAssignmentDomain = new DefiniteAssignmentDomain(locals);
        var singleAssignmentDomain = new SingleAssignmentDomain(locals);

        var domain = TupleDomain.Create(returnsOnAllPathsDomain, definiteAssignmentDomain, singleAssignmentDomain);
        var flowAnalysis = DataFlowAnalysis.Create(cfg, domain);

        return new(
            diagnostics,
            returnsOnAllPathsDomain,
            definiteAssignmentDomain,
            singleAssignmentDomain,
            flowAnalysis);
    }

    private readonly DiagnosticBag diagnostics;
    private readonly ReturnsOnAllPathsDomain returnsOnAllPathsDomain;
    private readonly DefiniteAssignmentDomain definiteAssignmentDomain;
    private readonly SingleAssignmentDomain singleAssignmentDomain;
    private readonly DataFlowAnalysis<(ReturnState Return, BitArray DefiniteAssignment, BitArray SingleAssignment)> analysis;

    private CompleteFlowAnalysis(
        DiagnosticBag diagnostics,
        ReturnsOnAllPathsDomain returnsOnAllPathsDomain,
        DefiniteAssignmentDomain definiteAssignmentDomain,
        SingleAssignmentDomain singleAssignmentDomain,
        DataFlowAnalysis<(ReturnState, BitArray, BitArray)> analysis)
    {
        this.diagnostics = diagnostics;
        this.returnsOnAllPathsDomain = returnsOnAllPathsDomain;
        this.definiteAssignmentDomain = definiteAssignmentDomain;
        this.singleAssignmentDomain = singleAssignmentDomain;
        this.analysis = analysis;
    }

    public override void VisitLocalExpression(BoundLocalExpression node)
    {
        base.VisitLocalExpression(node);

        var (_, localAssignment, _) = this.analysis.GetEntry(node);
        var isUnassigned = this.definiteAssignmentDomain.IsSet(localAssignment, node.Local);
        if (isUnassigned)
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

        var (_, localAssignment, singleAssignment) = this.analysis.GetEntry(node);

        // First we check, if we have a compound assignment
        if (node.CompoundOperator is not null)
        {
            // In this case, the left side is also a read first, needs to be assigned
            var isUnassigned = this.definiteAssignmentDomain.IsSet(localAssignment, localLvalue.Local);
            if (isUnassigned)
            {
                // Referencing an unassigned local, error
                this.diagnostics.Add(Diagnostic.Create(
                    template: FlowAnalysisErrors.VariableUsedBeforeInit,
                    location: node.Left.Syntax?.Location,
                    formatArgs: localLvalue.Local.Name));
            }
        }

        // If we have an immutable local, we need to check, if it's been assigned before
        // We allow the definition to make an assignment, as the reaching definition could leak back to it
        // It is not a problem, the other assignment should be reported as reassignment in that case
        if (!localLvalue.Local.IsMutable && node.Syntax is not VariableDeclarationSyntax)
        {
            var isAssigned = this.singleAssignmentDomain.IsSet(singleAssignment, localLvalue.Local);
            if (isAssigned)
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
