using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// The top state of the flow analysis, which is the "least defined" state.
    /// </summary>
    public abstract TState Top { get; }

    /// <summary>
    /// A clone function that creates a copy of the given state.
    /// </summary>
    /// <param name="state">The state to clone.</param>
    /// <returns>The cloned state.</returns>
    public virtual TState Clone(in TState state) => state;

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
    /// <returns>True if the state was changed, false otherwise.</returns>
    public bool TransferForward(ref TState state, IBasicBlock basicBlock)
    {
        var changed = false;
        foreach (var node in basicBlock) changed |= this.Transfer(ref state, node);
        return changed;
    }

    /// <summary>
    /// Transfers the state backward through the given basic block.
    /// </summary>
    /// <param name="state">The state to transfer.</param>
    /// <param name="basicBlock">The basic block to transfer the state through.</param>
    /// <returns>True if the state was changed, false otherwise.</returns>
    public bool TransferBackward(ref TState state, IBasicBlock basicBlock)
    {
        var changed = false;
        for (var i = basicBlock.Count - 1; i >= 0; --i) changed |= this.Transfer(ref state, basicBlock[i]);
        return changed;
    }
}

// TODO: Replace BitArray, it's horrible
/// <summary>
/// A gen-kill flow domain that uses a bit array to represent the state.
/// </summary>
/// <typeparam name="TElement">The type of elements the bits represent.</typeparam>
internal abstract class GenKillFlowDomain<TElement>(IEnumerable<TElement> elements) : FlowDomain<BitArray>
{
    /// <summary>
    /// The elements that the bits represent.
    /// </summary>
    public ImmutableArray<TElement> Elements { get; } = elements.ToImmutableArray();

    private readonly Dictionary<BoundNode, BitArray> genSets = [];
    private readonly Dictionary<BoundNode, BitArray> notKillSets = [];

    public override BitArray Clone(in BitArray state) => new(state);

    public override bool Transfer(ref BitArray state, BoundNode node)
    {
        var gen = this.Gen(node);
        var notKill = this.NotKill(node);

        // I hate you BitArray for needing to clone instead of reporting number of bits changed
        var oldState = this.Clone(in state);
        state = state.And(notKill).Or(gen);
        // And I hate you for needing to compare each bit
        for (var i = 0; i < state.Length; ++i)
        {
            if (state[i] != oldState[i]) return true;
        }
        return false;
    }

    /// <summary>
    /// Retrieves the gen set for the given node.
    /// </summary>
    /// <param name="node">The node to retrieve the gen set for.</param>
    /// <returns>The gen set for the node.</returns>
    private BitArray Gen(BoundNode node)
    {
        if (!this.genSets.TryGetValue(node, out var gen))
        {
            gen = this.ComputeGen(node);
            this.genSets.Add(node, gen);
        }
        return gen;
    }

    /// <summary>
    /// Retrieves the kill set for the given node.
    /// </summary>
    /// <param name="node">The node to retrieve the kill set for.</param>
    /// <returns>The kill set for the node.</returns>
    private BitArray NotKill(BoundNode node)
    {
        if (!this.notKillSets.TryGetValue(node, out var notKill))
        {
            notKill = this.ComputeKill(node).Not();
            this.notKillSets.Add(node, notKill);
        }
        return notKill;
    }

    /// <summary>
    /// Constructs the gen set for the given node.
    /// </summary>
    /// <param name="node">The node to construct the gen set for.</param>
    /// <returns>The gen set for the node.</returns>
    protected abstract BitArray ComputeGen(BoundNode node);

    /// <summary>
    /// Constructsthe kill set for the given node.
    /// </summary>
    /// <param name="node">The node to construct the kill set for.</param>
    /// <returns>The kill set for the node.</returns>
    protected abstract BitArray ComputeKill(BoundNode node);
}
