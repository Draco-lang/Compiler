using System;
using System.Collections;
using System.Collections.Generic;
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
/// A mutable implementation of <see cref="IControlFlowGraph"/>.
/// </summary>
internal sealed class ControlFlowGraph : IControlFlowGraph
{
    public BasicBlock? Entry { get; set; }
    IBasicBlock IControlFlowGraph.Entry => this.Entry
                                        ?? throw new InvalidOperationException("the control flow graph has no entry point");

    public IEnumerable<IBasicBlock> AllBlocks => GraphTraversal.DepthFirst(
        start: (this as IControlFlowGraph).Entry,
        getNeighbors: n => n.Successors);
}
