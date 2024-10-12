using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A single basic block in a control flow graph.
/// </summary>
/// <param name="nodes">The bound nodes in the basic block.</param>
internal sealed class BasicBlock(ImmutableArray<BoundNode> nodes) : IReadOnlyList<BoundNode>
{
    public int Count => nodes.Length;
    public BoundNode this[int index] => nodes[index];

    public IEnumerator<BoundNode> GetEnumerator() => nodes.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
