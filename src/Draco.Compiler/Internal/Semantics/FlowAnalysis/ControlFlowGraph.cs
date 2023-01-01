using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Represents a control-flow view of some code segment.
/// </summary>
/// <typeparam name="TStatement">The individual statement type.</typeparam>
/// <typeparam name="TEdge">The edge type that goes between the basic-blocks.</typeparam>
internal interface IControlFlowGraph<TStatement, TEdge>
{
    /// <summary>
    /// The entry point.
    /// </summary>
    public IBasicBlock<TStatement, TEdge> Entry { get; }
}

/// <summary>
/// Represents a continuous sequence of <see cref="TStatement"/>s that are guaranteed to be executed one after another.
/// </summary>
/// <typeparam name="TStatement">The statement type.</typeparam>
/// <typeparam name="TEdge">The edge type that goes between blocks.</typeparam>
internal interface IBasicBlock<TStatement, TEdge>
{
    /// <summary>
    /// The statements within this block.
    /// </summary>
    public IReadOnlyList<TStatement> Statements { get; }

    /// <summary>
    /// The predecessor blocks of this one.
    /// </summary>
    public IEnumerable<KeyValuePair<TEdge, IBasicBlock<TStatement, TEdge>>> Predecessors { get; }

    /// <summary>
    /// The successor blocks of this one.
    /// </summary>
    public IEnumerable<KeyValuePair<TEdge, IBasicBlock<TStatement, TEdge>>> Successors { get; }
}
