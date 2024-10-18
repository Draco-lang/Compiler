using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.OptimizingIr.Instructions;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A single flow analysis that can be performed on a control flow graph.
/// </summary>
/// <typeparam name="TState">The state type of the domain used in the flow analysis.</typeparam>
internal sealed class FlowAnalysis<TState>
{
    /// <summary>
    /// Constructs a new flow analysis on the given control flow graph.
    /// </summary>
    /// <param name="cfg">The control flow graph to analyze.</param>
    /// <param name="domain">The domain of the flow analysis.</param>
    /// <returns>The created flow analysis.</returns>
    public static FlowAnalysis<TState> Create(IControlFlowGraph cfg, FlowDomain<TState> domain) => new(cfg, domain);

    /// <summary>
    /// The state of a single block.
    /// </summary>
    private sealed class BlockState(TState enter, TState exit)
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

    private FlowDirection Direction => this.domain.Direction;

    private readonly IControlFlowGraph cfg;
    private readonly FlowDomain<TState> domain;
    private readonly Dictionary<IBasicBlock, BlockState> blockStates = [];

    private FlowAnalysis(IControlFlowGraph cfg, FlowDomain<TState> domain)
    {
        this.cfg = cfg;
        this.domain = domain;
    }

    private void RunAnalysis()
    {
        if (this.Direction == FlowDirection.Forward)
        {
            // Initialize the entry block by setting the enter state to the initial state of the domain
            // and the exit state to the result of transferring the initial state through the block
            var entryState = this.GetBlockState(this.cfg.Entry);
            entryState.Enter = this.domain.Initial;
            entryState.Exit = this.TransferAndCopy(in entryState.Exit, this.cfg.Entry, out _);
        }
        else if (this.cfg.Exit is not null)
        {
            // Initialize the exit block by setting the exit state to the initial state of the domain
            // and the enter state to the result of transferring the initial state through the block
            var exitState = this.GetBlockState(this.cfg.Exit);
            exitState.Exit = this.domain.Initial;
            exitState.Enter = this.TransferAndCopy(in exitState.Enter, this.cfg.Exit, out _);
        }

        // The rest are initialized to the top state by default

        // Initialize the worklist with all blocks, except the entry and exit blocks
        var worklist = new Queue<IBasicBlock>(this.cfg.AllBlocks.Except([this.cfg.Entry, this.cfg.Exit!]));

        // Perform the analysis while we have blocks to process in the worklist
        while (worklist.TryDequeue(out var block))
        {
            var blockState = this.GetBlockState(block);
            if (this.Direction == FlowDirection.Forward)
            {
                // Compute the exit state of n by combining the enter states of its successors
                this.domain.Join(ref blockState.Exit, block.Successors.Select(e => this.GetBlockState(e.Successor).Enter));
                blockState.Enter = this.TransferAndCopy(blockState.Exit, block, out var changed);
                if (!changed) continue;

                // If there was a change, all the predecessors might change too, enqueue them
                foreach (var edge in block.Predecessors) worklist.Enqueue(edge.Predecessor);
            }
            else
            {
                // Compute the entry state of n by combining the exit states of its predecessors
                this.domain.Join(ref blockState.Enter, block.Predecessors.Select(e => this.GetBlockState(e.Predecessor).Exit));
                blockState.Exit = this.TransferAndCopy(blockState.Enter, block, out var changed);
                if (!changed) continue;

                // If there was a change, all the successors might change too, enqueue them
                foreach (var edge in block.Successors) worklist.Enqueue(edge.Successor);
            }
        }
    }

    private BlockState GetBlockState(IBasicBlock block)
    {
        if (!this.blockStates.TryGetValue(block, out var state))
        {
            state = new(this.domain.Top, this.domain.Top);
            this.blockStates.Add(block, state);
        }
        return state;
    }

    private TState TransferAndCopy(in TState state, IBasicBlock block, out bool changed)
    {
        var newState = this.domain.Clone(in state);
        changed = this.domain.Transfer(ref newState, block);
        return newState;
    }
}
