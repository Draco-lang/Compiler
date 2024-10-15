using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A control flow graph consisting of <see cref="IBasicBlock"/>s.
/// </summary>
internal interface IControlFlowGraph
{
    /// <summary>
    /// The entry point of the control flow graph.
    /// </summary>
    public IBasicBlock Entry { get; }

    /// <summary>
    /// All basic blocks in the control flow graph.
    /// </summary>
    public IEnumerable<IBasicBlock> AllBlocks { get; }

    /// <summary>
    /// Translates the control flow graph to a DOT graph.
    /// </summary>
    /// <returns>The DOT representation of the control flow graph.</returns>
    public string ToDot();
}

/// <summary>
/// An implementation of <see cref="IControlFlowGraph"/>.
/// </summary>
internal sealed class ControlFlowGraph(BasicBlock entry) : IControlFlowGraph
{
    public BasicBlock Entry { get; } = entry;
    IBasicBlock IControlFlowGraph.Entry => this.Entry;

    public IEnumerable<IBasicBlock> AllBlocks => GraphTraversal.DepthFirst(
        start: (this as IControlFlowGraph).Entry,
        getNeighbors: n => n.Successors.Select(e => e.Successor).Concat(n.Predecessors.Select(e => e.Predecessor)));

    public string ToDot()
    {
        var graph = new DotGraphBuilder<IBasicBlock>(isDirected: true);
        graph.WithName("ControlFlowGraph");

        foreach (var block in this.AllBlocks)
        {
            graph
                .AddVertex(block)
                .WithShape(DotAttribs.Shape.Rectangle)
                .WithHtmlAttribute("label", BasicBlockToLabel(block));

            foreach (var edge in block.Successors)
            {
                graph
                    .AddEdge(block, edge.Successor)
                    .WithLabel(EdgeToLabel(edge));
            }
            // NOTE: Adding the predecessor would just cause each edge to show up twice
        }

        return graph.ToDot();

        static string BasicBlockToLabel(IBasicBlock block)
        {
            var result = new StringBuilder();
            foreach (var node in block)
            {
                result.Append(BoundNodeToLabel(node));
                result.Append("<br align=\"left\"/>");
            }
            // NOTE: Empty HTML labels are not allowed by Graphviz, we just add a space
            if (result.Length == 0) result.Append(' ');
            return result.ToString();
        }

        static string BoundNodeToLabel(BoundNode node) =>
            node.GetType().Name.Replace("Bound", string.Empty);

        static string EdgeToLabel(SuccessorEdge edge) =>
            edge.Condition.Kind.ToString();
    }
}
