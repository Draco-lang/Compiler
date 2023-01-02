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
internal interface IControlFlowGraph<TStatement>
{
    /// <summary>
    /// The entry point.
    /// </summary>
    public IBasicBlock<TStatement> Entry { get; }

    /// <summary>
    /// The exit point.
    /// </summary>
    public IBasicBlock<TStatement> Exit { get; }

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

/// <summary>
/// Builder for CFGs.
/// </summary>
/// <typeparam name="TStatement">The statement type.</typeparam>
internal sealed class CfgBuilder<TStatement>
{
    /// <summary>
    /// Builds the control-flow graph.
    /// </summary>
    /// <returns>The built CFG.</returns>
    public IControlFlowGraph<TStatement> Build()
    {
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds a statement to the currently built CFG.
    /// </summary>
    /// <param name="statement">The statement to add.</param>
    public void AddStatement(TStatement statement)
    {
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Starts branching in the CFG. Needs to call <see cref="NextBranch"/> before any further <see cref="AddStatement(TStatement)"/>s.
    /// </summary>
    public void StartBranching()
    {
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Ends branching in the CFG.
    /// </summary>
    public void EndBranching()
    {
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Opens a new branch in the CFG.
    /// </summary>
    public void NextBranch()
    {
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Marks the current place in the CFG as a potential jump-target.
    /// </summary>
    /// <returns>The marker for the place.</returns>
    public object MarkPlace()
    {
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Connects the current flow of CFG to the given marked place.
    /// </summary>
    /// <param name="mark">The mark to connect to.</param>
    public void Jump(object mark)
    {
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Marks the current point in the CFG as an exit point.
    /// </summary>
    public void Exit()
    {
        // TODO
        throw new NotImplementedException();
    }
}
