using System;
using System.Collections.Generic;
using System.Text;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Utility for visualizing data-flow.
/// </summary>
internal static class DataFlowGraphPrinter
{
    public static string ToDot(DataFlowGraph dfg)
    {
        var graph = new DotGraphBuilder<DataFlowOperation>(isDirected: true);

        foreach (var op in dfg.Operations)
        {
            // Add vertex
            graph.AddVertex(op).WithLabel(op.Node.ParseNode?.ToString() ?? "No-op");

            // Add edges
            foreach (var succ in op.Successors) graph.AddEdge(op, succ);
        }

        return graph.ToDot();
    }
}
