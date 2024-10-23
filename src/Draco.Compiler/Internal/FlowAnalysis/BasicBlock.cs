using System.Collections;
using System.Collections.Generic;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A single edge in a control flow graph pointing from a predecessor to the current basic block.
/// </summary>
/// <param name="Condition">The condition on the edge.</param>
/// <param name="Predecessor">The predecessor basic block.</param>
internal readonly record struct PredecessorEdge(FlowCondition Condition, IBasicBlock Predecessor);

/// <summary>
/// A single edge in a control flow graph pointing from the current basic block to a successor.
/// </summary>
/// <param name="Condition">The condition on the edge.</param>
/// <param name="Successor">The successor basic block.</param>
internal readonly record struct SuccessorEdge(FlowCondition Condition, IBasicBlock Successor);

/// <summary>
/// A single basic block in a control flow graph.
/// </summary>
internal interface IBasicBlock : IReadOnlyList<BoundNode>
{
    /// <summary>
    /// The predecessors of the basic block.
    /// </summary>
    public IEnumerable<PredecessorEdge> Predecessors { get; }

    /// <summary>
    /// The successors of the basic block.
    /// </summary>
    public IEnumerable<SuccessorEdge> Successors { get; }
}

/// <summary>
/// A mutable implementation of <see cref="IBasicBlock"/>.
/// </summary>
internal sealed class BasicBlock : IBasicBlock
{
    public List<BoundNode> Nodes { get; } = [];
    public ISet<PredecessorEdge> Predecessors { get; } = new HashSet<PredecessorEdge>();
    public ISet<SuccessorEdge> Successors { get; } = new HashSet<SuccessorEdge>();

    IEnumerable<PredecessorEdge> IBasicBlock.Predecessors => this.Predecessors;
    IEnumerable<SuccessorEdge> IBasicBlock.Successors => this.Successors;

    public int Count => this.Nodes.Count;
    public BoundNode this[int index] => this.Nodes[index];

    public IEnumerator<BoundNode> GetEnumerator() => this.Nodes.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
