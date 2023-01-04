using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Represents a control-flow view of some code segment.
/// </summary>
/// <typeparam name="TStatement">The individual statement type.</typeparam>
internal interface IControlFlowGraph<TStatement>
{
    /// <summary>
    /// The entry point.
    /// </summary>
    public IBasicBlock<TStatement> Entry { get; }

    /// <summary>
    /// The exit point(s).
    /// </summary>
    public IEnumerable<IBasicBlock<TStatement>> Exit { get; }

    /// <summary>
    /// The basic-blocks within this graph.
    /// </summary>
    public IEnumerable<IBasicBlock<TStatement>> Blocks { get; }
}

/// <summary>
/// Represents a continuous sequence of <see cref="TStatement"/>s that are guaranteed to be executed one after another.
/// </summary>
/// <typeparam name="TStatement">The statement type.</typeparam>
internal interface IBasicBlock<TStatement>
{
    /// <summary>
    /// The statements within this block.
    /// </summary>
    public IReadOnlyList<TStatement> Statements { get; }

    /// <summary>
    /// The predecessor blocks of this one.
    /// </summary>
    public IEnumerable<IBasicBlock<TStatement>> Predecessors { get; }

    /// <summary>
    /// The successor blocks of this one.
    /// </summary>
    public IEnumerable<IBasicBlock<TStatement>> Successors { get; }
}
