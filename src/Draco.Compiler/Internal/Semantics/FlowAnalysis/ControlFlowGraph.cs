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

/// <summary>
/// Builder for CFGs.
/// </summary>
/// <typeparam name="TStatement">The statement type.</typeparam>
internal sealed class CfgBuilder<TStatement>
{
    internal sealed class Cfg : IControlFlowGraph<TStatement>
    {
        public IBasicBlock<TStatement> Entry { get; set; } = null!;
        public List<IBasicBlock<TStatement>> Exit { get; } = new();
        IEnumerable<IBasicBlock<TStatement>> IControlFlowGraph<TStatement>.Exit => this.Exit;
        public IEnumerable<IBasicBlock<TStatement>> Blocks => GraphTraversal.DepthFirst(
            start: this.Entry,
            getNeighbors: b => b.Successors);
    }

    internal sealed class BasicBlock : IBasicBlock<TStatement>
    {
        public List<TStatement> Statements { get; } = new();
        public List<IBasicBlock<TStatement>> Predecessors { get; } = new();
        public List<IBasicBlock<TStatement>> Successors { get; } = new();

        IReadOnlyList<TStatement> IBasicBlock<TStatement>.Statements => this.Statements;
        IEnumerable<IBasicBlock<TStatement>> IBasicBlock<TStatement>.Predecessors => this.Predecessors;
        IEnumerable<IBasicBlock<TStatement>> IBasicBlock<TStatement>.Successors => this.Successors;
    }

    public readonly record struct Label(BasicBlock Block);

    private readonly record struct Context(BasicBlock Predecessor, List<BasicBlock> Branches);

    private readonly Cfg cfg;
    private readonly Stack<Context> contextStack = new();
    private BasicBlock currentBlock;

    public CfgBuilder()
    {
        this.cfg = new();
        this.currentBlock = new();
        this.cfg.Entry = this.currentBlock;
    }

    /// <summary>
    /// Builds the control-flow graph.
    /// </summary>
    /// <returns>The built CFG.</returns>
    public IControlFlowGraph<TStatement> Build()
    {
        if (this.contextStack.Count > 0) throw new InvalidOperationException("not all branching is closed");
        return this.cfg;
    }

    /// <summary>
    /// Adds a statement to the currently built CFG.
    /// </summary>
    /// <param name="statement">The statement to add.</param>
    public void AddStatement(TStatement statement)
    {
        this.AssertValidContext();
        this.currentBlock.Statements.Add(statement);
    }

    /// <summary>
    /// Starts branching in the CFG. Needs to call <see cref="NextBranch"/> before any further <see cref="AddStatement(TStatement)"/>s.
    /// </summary>
    public void StartBranching()
    {
        this.AssertValidContext();
        this.contextStack.Push(new(this.currentBlock, new()));
        this.currentBlock = null!;
    }

    /// <summary>
    /// Ends branching in the CFG.
    /// </summary>
    public void EndBranching()
    {
        this.AssertValidContext();
        if (!this.contextStack.TryPop(out var ctx)) throw new InvalidOperationException("not in branching");
        var successor = new BasicBlock();
        foreach (var block in ctx.Branches) Connect(block, successor);
        this.currentBlock = successor;
    }

    /// <summary>
    /// Opens a new branch in the CFG.
    /// </summary>
    public void NextBranch()
    {
        if (!this.contextStack.TryPeek(out var ctx)) throw new InvalidOperationException("not in branching");
        if (this.currentBlock is not null) ctx.Branches.Add(this.currentBlock);
        this.currentBlock = new BasicBlock();
        Connect(ctx.Predecessor, this.currentBlock);
    }

    /// <summary>
    /// Declares a label that can be placed later.
    /// </summary>
    /// <returns>The declared label.</returns>
    public Label DeclareLabel() => new(new());

    /// <summary>
    /// Marks the current place in the CFG as a potential jump-target.
    /// </summary>
    /// <returns>The label for the place.</returns>
    public Label PlaceLabel()
    {
        this.AssertValidContext();
        var newLabel = this.DeclareLabel();
        this.ConnectToLabel(newLabel);
        return newLabel;
    }

    /// <summary>
    /// Connects the current flow of CFG to the given marked place.
    /// The current flow will not jump to the mark.
    /// </summary>
    /// <param name="mark">The mark to connect to.</param>
    public void ConnectToLabel(Label mark)
    {
        this.AssertValidContext();
        Connect(this.currentBlock, mark.Block);
    }

    /// <summary>
    /// Jumps the current flow of CFG to the given marked place.
    /// </summary>
    /// <param name="mark">The mark to jump to.</param>
    public void PlaceLabel(Label mark)
    {
        this.AssertValidContext();
        this.ConnectToLabel(mark);
        this.currentBlock = mark.Block;
    }

    /// <summary>
    /// Marks the current point in the CFG as an exit point.
    /// </summary>
    public void Exit()
    {
        this.AssertValidContext();
        this.cfg.Exit.Add(this.currentBlock);
    }

    private void AssertValidContext()
    {
        if (this.currentBlock is null)
        {
            throw new InvalidOperationException("no branches opened after branching operation");
        }
    }

    private static void Connect(BasicBlock pred, BasicBlock succ)
    {
        pred.Successors.Add(succ);
        succ.Predecessors.Add(pred);
    }
}
