using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A single basic block in a control flow graph.
/// </summary>
internal interface IBasicBlock : IReadOnlyList<BoundNode>
{
    /// <summary>
    /// The predecessors of the basic block.
    /// </summary>
    public IEnumerable<BasicBlock> Predecessors { get; }

    /// <summary>
    /// The successors of the basic block.
    /// </summary>
    public IEnumerable<BasicBlock> Successors { get; }
}

/// <summary>
/// A mutable implementation of <see cref="IBasicBlock"/>.
/// </summary>
internal sealed class BasicBlock(ImmutableArray<BoundNode> nodes) : IBasicBlock
{
    public ISet<BasicBlock> Predecessors { get; } = new HashSet<BasicBlock>();
    public ISet<BasicBlock> Successors { get; } = new HashSet<BasicBlock>();

    IEnumerable<BasicBlock> IBasicBlock.Predecessors => this.Predecessors;
    IEnumerable<BasicBlock> IBasicBlock.Successors => this.Successors;

    public int Count => nodes.Length;
    public BoundNode this[int index] => nodes[index];

    public IEnumerator<BoundNode> GetEnumerator() => nodes.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
