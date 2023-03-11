using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// A graph of <see cref="DataFlowOperation"/>s for analysis.
/// </summary>
/// <param name="Entry">The entry point.</param>
/// <param name="Exit">The exit points.</param>
internal sealed record class DataFlowGraph(
    DataFlowOperation Entry,
    ImmutableArray<DataFlowOperation> Exit)
{
    /// <summary>
    /// All <see cref="DataFlowOperation"/>s in this graph.
    /// </summary>
    public IEnumerable<DataFlowOperation> Operations => GraphTraversal.DepthFirst(
        start: this.Entry,
        getNeighbors: op => op.Predecessors.Concat(op.Successors).Distinct());

    /// <summary>
    /// Converts the data-flow graph to a DOT graph for debugging purposes.
    /// </summary>
    /// <returns>The DOT graph of the data-flow graph.</returns>
    public string ToDot()
    {
        var graph = new DotGraphBuilder<DataFlowOperation>(isDirected: true);

        foreach (var op in this.Operations)
        {
            // Add vertex
            graph.AddVertex(op).WithLabel(op.Node.Syntax?.ToString() ?? "No-op");

            // Add edges
            foreach (var succ in op.Successors) graph.AddEdge(op, succ);
        }

        return graph.ToDot();
    }
}

/// <summary>
/// Represents a single operation during DFA.
/// </summary>
internal sealed class DataFlowOperation
{
    // NOTE: Mutable because of cycles...
    // TODO: Can we get rid of the mutability at least on onterface level?
    /// <summary>
    /// The <see cref="BoundNode"/> node corresponding to the operation.
    /// </summary>
    public BoundNode Node { get; set; }

    /// <summary>
    /// The predecessor operations of this one.
    /// </summary>
    public ISet<DataFlowOperation> Predecessors { get; } = new HashSet<DataFlowOperation>();

    /// <summary>
    /// The successor operations of this one.
    /// </summary>
    public ISet<DataFlowOperation> Successors { get; } = new HashSet<DataFlowOperation>();

    public DataFlowOperation(BoundNode node)
    {
        this.Node = node;
    }

    public static void Join(DataFlowOperation first, DataFlowOperation second)
    {
        first.Successors.Add(second);
        second.Predecessors.Add(first);
    }
}
