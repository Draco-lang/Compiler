using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.FlowAnalysis.Lattices;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Accumulates all data-flow passes as one.
/// </summary>
internal sealed class DataFlowPasses : AstVisitorBase<Unit>
{
    /// <summary>
    /// Performs all DFA analysis on the given AST.
    /// </summary>
    /// <param name="ast">The AST to perform the analysis on.</param>
    /// <returns>The list of <see cref="Diagnostic"/>s produced during analysis.</returns>
    public static ImmutableArray<Diagnostic> Analyze(Ast ast)
    {
        var passes = new DataFlowPasses();
        passes.Visit(ast);
        return passes.diagnostics.ToImmutable();
    }

    private readonly ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    private DataFlowPasses()
    {
    }

    public override Unit VisitFuncDecl(Ast.Decl.Func node)
    {
        base.VisitFuncDecl(node);

        var graph = AstToDataFlowGraph.ToDataFlowGraph(node.Body);

        // TODO: Temporary
        // Console.WriteLine(DataFlowGraphPrinter.ToDot(graph));

        this.CheckReturnsOnAllPaths(node, graph);
        this.CheckIfOnlyInitializedVariablesAreUsed(graph);

        return this.Default;
    }

    private void CheckReturnsOnAllPaths(Ast.Decl.Func node, DataFlowGraph graph)
    {
        // We check if all operations without a successor are a return
        var allReturns = graph.Operations
            .Where(op => op.Successors.Count == 0)
            .All(op => op.Node is Ast.Expr.Return);
        if (!allReturns)
        {
            // Does not return on all paths
            this.diagnostics.Add(Diagnostic.Create(
                template: SemanticErrors.DoesNotReturn,
                location: node.DeclarationSymbol.Definition?.Location ?? Location.None,
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
            if (node is not Ast.Expr.Reference r) continue;
            if (r.Symbol.IsError) continue;
            if (r.Symbol is not ISymbol.IVariable var) continue;
            if (var.IsGlobal || var is ISymbol.IParameter) continue;

            if (info.In[var] != DefiniteAssignment.Status.Initialized)
            {
                // Use of uninitialized variable
                this.diagnostics.Add(Diagnostic.Create(
                template: SemanticErrors.VariableUsedBeforeInit,
                location: node.ParseNode?.Location ?? Location.None,
                formatArgs: var.Name));
            }
        }
    }
}
