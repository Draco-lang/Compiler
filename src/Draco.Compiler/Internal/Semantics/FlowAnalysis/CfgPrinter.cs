using System;
using System.Collections.Generic;
using System.Text;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Printing utilities for <see cref="IControlFlowGraph{TStatement}"/>s.
/// </summary>
internal static class CfgPrinter
{
    /// <summary>
    /// Converts the given CFG to a dot-graph representation.
    /// </summary>
    /// <typeparam name="TStatement">The statement type.</typeparam>
    /// <param name="cfg">The CFG to convert.</param>
    /// <param name="bbToString">The function to stringify the basic-blocks.</param>
    /// <returns>The CFG as a DOT graph.</returns>
    public static string ToDot<TStatement>(
        IControlFlowGraph<TStatement> cfg,
        Func<IBasicBlock<TStatement>, string>? bbToString = null)
    {
        var graph = new DotGraphBuilder<IBasicBlock<TStatement>>(isDirected: true);
        graph.WithName("CFG");
        graph.AllVertices().WithShape(DotAttribs.Shape.Rectangle);

        // Add label, if needed
        if (bbToString is not null)
        {
            foreach (var block in cfg.Blocks) graph.AddVertex(block).WithLabel(bbToString(block));
        }

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
