using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
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
    /// The exit point of the control flow graph, if any.
    /// </summary>
    public IBasicBlock? Exit { get; }

    /// <summary>
    /// All basic blocks in the control flow graph.
    /// </summary>
    public IEnumerable<IBasicBlock> AllBlocks { get; }
}

/// <summary>
/// An implementation of <see cref="IControlFlowGraph"/>.
/// </summary>
internal sealed class ControlFlowGraph(BasicBlock entry, BasicBlock? exit) : IControlFlowGraph
{
    public BasicBlock Entry { get; } = entry;
    IBasicBlock IControlFlowGraph.Entry => this.Entry;

    public BasicBlock? Exit { get; } = exit;
    IBasicBlock? IControlFlowGraph.Exit => this.Exit;

    public IEnumerable<BasicBlock> AllBlocks => GraphTraversal.DepthFirst(
        start: this.Entry,
        getNeighbors: n => n.Successors
            .Select(e => e.Successor)
            .Concat(n.Predecessors.Select(e => e.Predecessor))
            .Cast<BasicBlock>());
    IEnumerable<IBasicBlock> IControlFlowGraph.AllBlocks => this.AllBlocks;
}
