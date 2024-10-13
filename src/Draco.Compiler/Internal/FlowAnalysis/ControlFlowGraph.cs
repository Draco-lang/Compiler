using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        getNeighbors: n => n.Successors.Concat(n.Predecessors));
}
