using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A base class for implementing dataflow passes.
/// </summary>
/// <typeparam name="TState">The state being tracked by the pass.</typeparam>
internal abstract class FlowAnalysisPass<TState> : BoundTreeVisitor
{
    // Lattice operations //////////////////////////////////////////////////////

    /// <summary>
    /// The top element of this lattice, which generally represents "reachable, but no information yet",
    /// which is usually the state of the starting point.
    /// </summary>
    public abstract TState Top { get; }

    /// <summary>
    /// The bottom element of this lattice, usually representing "unreachable".
    /// </summary>
    public abstract TState Bottom { get; }

    /// <summary>
    /// Joins two elements of the lattice, usually corresponding to multiple paths
    /// converging at the same point. Also known as the "least upper bound".
    ///
    /// Rules to hold:
    ///  1) Join(Bottom, X) = X
    ///  2) Join(Top, X) = Top
    /// </summary>
    /// <param name="target">The target state to join into.</param>
    /// <param name="other">The state to merge into <paramref name="target"/>.</param>
    /// <returns>True, if <paramref name="target"/> was changed, false otherwise.</returns>
    public abstract bool Join(ref TState target, in TState other);

    /// <summary>
    /// Additively combines two elements of the lattice.
    /// Also known as the "greatest lower bound".
    ///
    /// Rules to hold:
    ///  1) Meet(Bottom, X) = Bottom
    ///  2) Meet(Top, X) = X
    /// </summary>
    /// <param name="target">The target state to meet into.</param>
    /// <param name="other">The state to merge into <paramref name="target"/>.</param>
    /// <returns>True, if <paramref name="target"/> was changed, false otherwise.</returns>
    public abstract bool Meet(ref TState target, in TState other);

    /// <summary>
    /// Deep-copies the given state.
    /// </summary>
    /// <param name="state">The state to deep-copy.</param>
    /// <returns>An equivalent clone of <paramref name="state"/>.</returns>
    public abstract TState Clone(in TState state);

    // Flow analysis related things ////////////////////////////////////////////

    /// <summary>
    /// Represents a join operation that is yet to be performed.
    /// </summary>
    /// <param name="Label">The label to join at.</param>
    /// <param name="State">The state to join.</param>
    private readonly record struct PendingJoin(LabelSymbol Label, TState State);

    // NOTE: This is a field for a reason, we pass refs to this
    /// <summary>
    /// The current state.
    /// </summary>
    protected TState State;

    /// <summary>
    /// True, if a change occurred that justifies iterating once more.
    /// </summary>
    protected bool HasChanged;

    // State for each label
    private readonly Dictionary<LabelSymbol, TState> labeledStates = new();
    // Joins into labels that we have jumped to
    private readonly List<PendingJoin> pendingJoins = new();

    protected FlowAnalysisPass()
    {
        // Assume beginning is reachable
        this.State = this.Top;
    }

    public TState Analyze(BoundNode node)
    {
        do
        {
            this.State = this.Top;
            this.HasChanged = false;
            node.Accept(this);
            this.JoinPending();
        } while (this.HasChanged);
        return this.State;
    }

    private void JoinPending()
    {
        foreach (var (label, state) in this.pendingJoins)
        {
            if (this.labeledStates.TryGetValue(label, out var oldState))
            {
                // Potential update
                if (this.Join(ref oldState, state))
                {
                    this.labeledStates[label] = oldState;
                    this.HasChanged = true;
                }
            }
            else
            {
                // No update
                this.labeledStates.Add(label, state);
            }
        }
        this.pendingJoins.Clear();
    }

    private void LoopHead(LabelSymbol continueLabel)
    {
        if (this.labeledStates.TryGetValue(continueLabel, out var prevState))
        {
            this.Join(ref this.State, in prevState);
        }
        this.labeledStates[continueLabel] = this.Clone(in this.State);
    }

    private void LoopTail(LabelSymbol continueLabel)
    {
        var prevState = this.labeledStates[continueLabel];
        if (this.Join(ref prevState, in this.State))
        {
            this.labeledStates[continueLabel] = prevState;
            this.HasChanged = true;
        }
    }

    public override void VisitConditionalGotoStatement(BoundConditionalGotoStatement node) =>
        throw new InvalidOperationException("flow analysis should run on the non-lowered bound tree");

    public override void VisitIfExpression(BoundIfExpression node)
    {
        // First, the condition always executes
        this.VisitExpression(node.Condition);
        // Then we have two alternatives, so we save
        var elseState = this.Clone(this.State);
        // We run the 'then' alternative
        this.VisitExpression(node.Then);
        var thenState = this.State;
        // Then we run the 'else' alternative from after the condition
        this.State = elseState;
        this.VisitExpression(node.Else);
        // Finally, we merge
        this.Join(ref this.State, in thenState);
    }

    public override void VisitWhileExpression(BoundWhileExpression node)
    {
        // We join in with the continue label
        this.LoopHead(node.ContinueLabel);
        // Condition always gets evaluated
        this.VisitExpression(node.Condition);
        // Body does not necessarily run
        var breakState = this.Clone(in this.State);
        // We continue with the looping, run body
        this.VisitExpression(node.Then);
        // Loop back to continue
        this.LoopTail(node.ContinueLabel);
        // We continue with the break state
        this.State = breakState;
    }

    public override void VisitLabelStatement(BoundLabelStatement node)
    {
        // Look up the previously saved label state
        if (this.labeledStates.TryGetValue(node.Label, out var prevState))
        {
            // Join it in
            this.Join(ref this.State, in prevState);
        }
        // Save a copy of this new state for the label
        this.labeledStates[node.Label] = this.Clone(in this.State);
    }

    public override void VisitGotoExpression(BoundGotoExpression node)
    {
        // Register a pending join
        this.pendingJoins.Add(new(Label: node.Target, this.Clone(in this.State)));
        // Detach
        this.State = this.Bottom;
    }

    public override void VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        // We fix ordering
        node.Right.Accept(this);
        node.Left.Accept(this);
    }

    public override void VisitAndExpression(BoundAndExpression node)
    {
        // The left is always evaluated
        this.VisitExpression(node.Left);
        // The right is optional
        var rightNotEvaluated = this.Clone(in this.State);
        // Evaluate right
        this.VisitExpression(node.Right);
        // Join in
        this.Join(ref this.State, in rightNotEvaluated);
    }

    public override void VisitOrExpression(BoundOrExpression node)
    {
        // The left is always evaluated
        this.VisitExpression(node.Left);
        // The right is optional
        var rightNotEvaluated = this.Clone(in this.State);
        // Evaluate right
        this.VisitExpression(node.Right);
        // Join in
        this.Join(ref this.State, in rightNotEvaluated);
    }

    public override void VisitRelationalExpression(BoundRelationalExpression node)
    {
        // The first two operands are always evaluated
        this.VisitExpression(node.First);
        this.VisitExpression(node.Comparisons[0].Next);
        // After that, everything else is optional
        var notEvaluatedBranches = new List<TState>();
        for (var i = 1; i < node.Comparisons.Length; ++i)
        {
            var state = this.Clone(in this.State);
            notEvaluatedBranches.Add(state);
            // Continue on with the main branch
            this.VisitExpression(node.Comparisons[i].Next);
        }
        // Join in with the main branch
        foreach (var branch in notEvaluatedBranches) this.Join(ref this.State, in branch);
    }

    public override void VisitReturnExpression(BoundReturnExpression node)
    {
        node.Value.Accept(this);
        // Detach
        this.State = this.Bottom;
    }
}
