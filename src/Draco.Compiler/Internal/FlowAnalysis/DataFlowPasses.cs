using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.FlowAnalysis.Lattices;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

// TODO: This is definitely not incremental
// We don't care for now, but later the flow graph construction and the passes should become incremental
// It should not be a big code-shift

/// <summary>
/// Accumulates all data-flow passes as one.
/// </summary>
internal sealed class DataFlowPasses : BoundTreeVisitor
{
    /// <summary>
    /// Performs all DFA analysis on the given bound tree.
    /// </summary>
    /// <param name="node">The bound tree to perform the analysis on.</param>
    /// <returns>The list of <see cref="Diagnostic"/>s produced during analysis.</returns>
    public static ImmutableArray<Diagnostic> Analyze(BoundNode node)
    {
        var passes = new DataFlowPasses();
        node.Accept(passes);
        return passes.diagnostics.ToImmutable();
    }

    private readonly ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    private DataFlowPasses()
    {
    }

    public override void VisitLocalDeclaration(BoundLocalDeclaration node)
    {
        base.VisitLocalDeclaration(node);
        this.CheckIfValIsInitialized(node);
    }

    public override void VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        base.VisitAssignmentExpression(node);
        this.CheckIfValIsNotAssigned(node);
    }

    // TODO: We'll need to make this a recursor on a module symbol
    // instead of just being a bound tree visitor
    // Since functions are not considered in visitation anymore
    /*
    public override Unit VisitFuncDecl(Ast.Decl.Func node)
    {
        base.VisitFuncDecl(node);

        var graph = BoundTreeToDataFlowGraph.ToDataFlowGraph(node.Body);

        this.CheckReturnsOnAllPaths(node, graph);
        this.CheckIfOnlyInitializedVariablesAreUsed(graph);

        return this.Default;
    }
    */

    private void CheckIfValIsInitialized(BoundLocalDeclaration node)
    {
        if (node.Local.IsMutable) return;
        if (node.Value is not null) return;

        // Not initialized
        this.diagnostics.Add(Diagnostic.Create(
            template: DataflowErrors.ImmutableVariableMustBeInitialized,
            location: node.Syntax?.Location,
            formatArgs: node.Local.Name));
    }

    private void CheckIfValIsNotAssigned(BoundAssignmentExpression node)
    {
        if (node.Left is not BoundLocalLvalue reference) return;
        if (reference.Local.IsMutable) return;

        // Immutable and modified
        this.diagnostics.Add(Diagnostic.Create(
            template: DataflowErrors.ImmutableVariableCanNotBeAssignedTo,
            location: node.Syntax?.Location,
            formatArgs: reference.Local.Name));
    }

    private void CheckReturnsOnAllPaths(Ast.Decl.Func node, DataFlowGraph graph)
    {
        // We check if all operations without a successor are a return
        var allReturns = graph.Operations
            .Where(op => op.Successors.Count == 0)
            .All(op => op.Node is BoundReturnExpression);
        if (!allReturns)
        {
            // Does not return on all paths
            this.diagnostics.Add(Diagnostic.Create(
                template: DataflowErrors.DoesNotReturn,
                location: node.DeclarationSymbol.Definition?.Location,
                formatArgs: node.DeclarationSymbol.Name));
        }
    }

    private void CheckIfOnlyInitializedVariablesAreUsed(DataFlowGraph graph)
    {
        var infos = DataFlowAnalysis.Analyze(
            lattice: DefiniteAssignment.Instance,
            graph: graph);
        foreach (var (node, info) in infos)
        {
            // We only care about references that reference local variables
            if (node is not BoundLocalExpression localExpr) continue;

            var local = localExpr.Local;
            if (info.In[local] != DefiniteAssignment.Status.Initialized)
            {
                // Use of uninitialized variable
                this.diagnostics.Add(Diagnostic.Create(
                    template: DataflowErrors.VariableUsedBeforeInit,
                    location: node.Syntax?.Location,
                    formatArgs: local.Name));
            }
        }
    }
}
