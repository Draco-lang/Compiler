using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Printing utilities for <see cref="IControlFlowGraph{TStatement}"/>s.
/// </summary>
internal static class CfgPrinter
{
    // TODO: Doc
    public static string ToDot(IControlFlowGraph<DracoIr.IReadOnlyInstruction> cfg) => ToDot(
        cfg: cfg,
        stmtToString: instr => instr.ToString());

    // TODO: Doc
    public static string ToDot(IControlFlowGraph<AbstractSyntax.Ast> cfg) => ToDot(
        cfg: cfg,
        stmtToString: AstToString);

    private static string AstToString(AbstractSyntax.Ast ast) => ast switch
    {
        AbstractSyntax.Ast.Expr.If @if => $"if ({@if.Condition.ParseNode})",
        AbstractSyntax.Ast.Expr.While @while => $"if ({@while.Condition.ParseNode})",
        _ => ast.ParseNode?.ToString() ?? string.Empty,
    };

    private static string ToDot<TStatement>(
        IControlFlowGraph<TStatement> cfg,
        Func<TStatement, string> stmtToString) =>
        ToDot(cfg, bb => string.Join(string.Empty, bb.Statements.Select(s => $"{stmtToString(s)}\n")));

    private static string ToDot<TStatement>(
        IControlFlowGraph<TStatement> cfg,
        Func<IBasicBlock<TStatement>, string> bbToString)
    {
        var graph = new DotGraphBuilder<IBasicBlock<TStatement>>(isDirected: true);
        graph.WithName("CFG");
        graph.AllVertices().WithShape(DotAttribs.Shape.Rectangle);

        // Add labels
        foreach (var block in cfg.Blocks) graph.AddVertex(block).WithLabel(bbToString(block));

        // Connect blocks
        foreach (var block in cfg.Blocks)
        {
            foreach (var succ in block.Successors) graph.AddEdge(block, succ);
        }

        // Label entry and exits
        graph.AddVertex(cfg.Entry).WithXLabel("ENTRY");
        foreach (var exit in cfg.Exit) graph.AddVertex(exit).WithXLabel("EXIT");

        return graph.ToDot();
    }
}
