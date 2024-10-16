using System.Collections.Generic;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Represents a domain of values that can be used in a flow analysis.
/// </summary>
/// <typeparam name="TState">The state type of the flow analysis.</typeparam>
internal abstract class FlowDomain<TState>
{
    /// <summary>
    /// The initial state of the flow analysis.
    /// </summary>
    public abstract TState Initial { get; }

    /// <summary>
    /// A clone function that creates a copy of the given state.
    /// </summary>
    /// <param name="state">The state to clone.</param>
    /// <returns>The cloned state.</returns>
    public virtual TState Clone(TState state) => state;

    /// <summary>
    /// The transfer function that updates the state based on the given node.
    /// </summary>
    /// <param name="state">The state to update.</param>
    /// <param name="node">The node to use for updating the state.</param>
    /// <returns>True if the state was changed, false otherwise.</returns>
    public abstract bool Transfer(ref TState state, BoundNode node);

    /// <summary>
    /// A join function that combines the given states into a single state.
    /// </summary>
    /// <param name="target">The target state to combine the sources into.</param>
    /// <param name="sources">The states to combine into the target.</param>
    /// <returns>True if the target state was changed, false otherwise.</returns>
    public abstract bool Join(ref TState target, IEnumerable<TState> sources);

    /// <summary>
    /// Transfers the state forward through the given basic block.
    /// </summary>
    /// <param name="state">The state to transfer.</param>
    /// <param name="basicBlock">The basic block to transfer the state through.</param>
    public void TransferForward(ref TState state, BasicBlock basicBlock)
    {
        foreach (var node in basicBlock.Nodes) this.Transfer(ref state, node);
    }

    /// <summary>
    /// Transfers the state backward through the given basic block.
    /// </summary>
    /// <param name="state">The state to transfer.</param>
    /// <param name="basicBlock">The basic block to transfer the state through.</param>
    public void TransferBackward(ref TState state, BasicBlock basicBlock)
    {
        for (var i = basicBlock.Nodes.Count - 1; i >= 0; --i) this.Transfer(ref state, basicBlock.Nodes[i]);
    }
}
