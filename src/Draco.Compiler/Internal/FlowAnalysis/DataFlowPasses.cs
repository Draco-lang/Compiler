using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.FlowAnalysis.Lattices;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis;

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
    /// <param name="module">The module to perform the analysis on.</param>
    /// <returns>The list of <see cref="Diagnostic"/>s produced during analysis.</returns>
    public static ImmutableArray<Diagnostic> Analyze(SourceModuleSymbol module)
    {
        var passes = new DataFlowPasses();
        passes.AnalyzeModule(module);
        return passes.diagnostics.ToImmutable();
    }

    private readonly ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    private DataFlowPasses()
    {
    }

    private void AnalyzeModule(SourceModuleSymbol module)
    {
        foreach (var symbol in module.Members)
        {
            if (symbol is SourceFunctionSymbol function) this.AnalyzeFunction(function);
        }
    }

    private void AnalyzeFunction(SourceFunctionSymbol function)
    {
        var graph = BoundTreeToDataFlowGraph.ToDataFlowGraph(function.Body);

        this.CheckReturnsOnAllPaths(function, graph);
        this.CheckIfOnlyInitializedVariablesAreUsed(graph);

        function.Body.Accept(this);
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

    private void CheckReturnsOnAllPaths(SourceFunctionSymbol function, DataFlowGraph graph)
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
                location: function.DeclarationSyntax.Location,
                formatArgs: function.Name));
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
