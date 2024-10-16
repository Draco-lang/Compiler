using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A single flow analysis that can be performed on a control flow graph.
/// </summary>
/// <typeparam name="TState">The state type of the domain used in the flow analysis.</typeparam>
internal abstract class FlowAnalysis<TState>(FlowDomain<TState> domain)
{
    /// <summary>
    /// The state of a single block.
    /// </summary>
    protected sealed class BlockState(TState enter, TState exit)
    {
        /// <summary>
        /// The entry state of the block.
        /// </summary>
        public TState Enter = enter;

        /// <summary>
        /// The exit state of the block.
        /// </summary>
        public TState Exit = exit;
    }

    /// <summary>
    /// The domain that is used in the flow analysis.
    /// </summary>
    public FlowDomain<TState> Domain { get; } = domain;

    private readonly Dictionary<IBasicBlock, BlockState> blockStates = [];

    /// <summary>
    /// Analyzes the given control flow graph and returns the state of the final block.
    /// </summary>
    /// <param name="cfg">The control flow graph to analyze.</param>
    /// <returns>The state of the final block.</returns>
    public abstract TState Analyze(ControlFlowGraph cfg);

    /// <summary>
    /// Clears the state of the flow analysis.
    /// </summary>
    protected void Clear() => this.blockStates.Clear();

    /// <summary>
    /// Retrieves the state of the given block.
    /// </summary>
    /// <param name="block">The block to retrieve the state for.</param>
    /// <returns>The state of the block.</returns>
    protected BlockState GetBlockState(IBasicBlock block)
    {
        if (!this.blockStates.TryGetValue(block, out var state))
        {
            state = new(this.Domain.Top, this.Domain.Top);
            this.blockStates.Add(block, state);
        }
        return state;
    }

    /// <summary>
    /// Transfers the state forward through the given basic block.
    /// </summary>
    /// <param name="state">The state to clone and transfer.</param>
    /// <param name="block">The basic block to transfer the state through.</param>
    /// <param name="changed">Whether the state changed.</param>
    /// <returns>The new state.</returns>
    protected TState TransferForward(in TState state, IBasicBlock block, out bool changed)
    {
        var newState = this.Domain.Clone(in state);
        changed = this.Domain.TransferForward(ref newState, block);
        return newState;
    }

    /// <summary>
    /// Transfers the state backward through the given basic block.
    /// </summary>
    /// <param name="state">The state to clone and transfer.</param>
    /// <param name="block">The basic block to transfer the state through.</param>
    /// <param name="changed">Whether the state changed.</param>
    /// <returns>The new state.</returns>
    protected TState TransferBackward(in TState state, IBasicBlock block, out bool changed)
    {
        var newState = this.Domain.Clone(in state);
        changed = this.Domain.TransferBackward(ref newState, block);
        return newState;
    }
}

/// <summary>
/// Implements a forward flow analysis on a control flow graph.
/// </summary>
/// <typeparam name="TState">The state type of the domain used in the flow analysis.</typeparam>
/// <param name="domain">The domain to use in the flow analysis.</param>
internal sealed class ForwardFlowAnalysis<TState>(FlowDomain<TState> domain)
    : FlowAnalysis<TState>(domain)
{
    public override TState Analyze(ControlFlowGraph cfg)
    {
        this.Clear();

        // Initialize the entry block by setting the enter state to the initial state of the domain
        // and the exit state to the result of transferring the initial state through the block
        var entryState = this.GetBlockState(cfg.Entry);
        entryState.Enter = this.Domain.Initial;
        entryState.Exit = this.TransferForward(in entryState.Exit, cfg.Entry, out _);

        // The rest are initialized to the top state by default

        // Initialize the worklist with all blocks, except the entry and exit blocks
        var worklist = new Queue<IBasicBlock>(cfg.AllBlocks.Except([cfg.Entry, cfg.Exit!]));

        // Perform the analysis
        while (worklist.TryDequeue(out var block))
        {
            // Compute the entry state of n by combining the exit states of its predecessors
            var blockState = this.GetBlockState(block);
            this.Domain.Join(ref blockState.Enter, block.Predecessors.Select(e => this.GetBlockState(e.Predecessor).Exit));
            blockState.Exit = this.TransferForward(blockState.Enter, block, out var changed);
            if (!changed) continue;

            // If there was a change, all the successors might change too, enqueue them
            foreach (var edge in block.Successors) worklist.Enqueue(edge.Successor);
        }

        return cfg.Exit is null
            ? this.Domain.Top
            : this.GetBlockState(cfg.Exit).Exit;
    }
}

/// <summary>
/// Implements a forward flow analysis on a control flow graph.
/// </summary>
/// <typeparam name="TState"></typeparam>
/// <param name="domain"></param>
internal sealed class BackwardFlowAnalysis<TState>(FlowDomain<TState> domain)
    : FlowAnalysis<TState>(domain)
{
    public override TState Analyze(ControlFlowGraph cfg)
    {
        this.Clear();

        // TODO: What to do with inconditional infinite loops?
        // We might still want to do the analysis, but we need a good starting point
        if (cfg.Exit is null) return this.Domain.Top;

        // Initialize the exit block by setting the exit state to the initial state of the domain
        // and the enter state to the result of transferring the initial state through the block
        var exitState = this.GetBlockState(cfg.Exit);
        exitState.Exit = this.Domain.Initial;
        exitState.Enter = this.TransferBackward(in exitState.Enter, cfg.Exit, out _);

        // The rest are initialized to the top state by default

        // Initialize the worklist with all blocks, except the entry and exit blocks
        var worklist = new Queue<IBasicBlock>(cfg.AllBlocks.Except([cfg.Entry, cfg.Exit]));

        // Perform the analysis
        while (worklist.TryDequeue(out var block))
        {
            // Compute the exit state of n by combining the enter states of its successors
            var blockState = this.GetBlockState(block);
            this.Domain.Join(ref blockState.Exit, block.Successors.Select(e => this.GetBlockState(e.Successor).Enter));
            blockState.Enter = this.TransferBackward(blockState.Exit, block, out var changed);
            if (!changed) continue;

            // If there was a change, all the predecessors might change too, enqueue them
            foreach (var edge in block.Predecessors) worklist.Enqueue(edge.Predecessor);
        }

        return this.GetBlockState(cfg.Entry).Enter;
    }
}
