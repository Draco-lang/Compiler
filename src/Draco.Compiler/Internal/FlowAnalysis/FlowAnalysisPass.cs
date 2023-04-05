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
    // Lattice operations

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

    // Flow analysis related things

    // NOTE: This is a field for a reason, we pass refs to this
    /// <summary>
    /// The current state.
    /// </summary>
    protected TState State;

    /// <summary>
    /// True, if a change occurred that justifies iterating once more.
    /// </summary>
    protected bool HasChanged;

    private readonly Dictionary<LabelSymbol, TState> labeledStates = new();

    protected FlowAnalysisPass()
    {
        // Assume beginning is reachable
        this.State = this.Top;
    }

    private TState GetLabelState(LabelSymbol label)
    {
        if (!this.labeledStates.TryGetValue(label, out var state))
        {
            state = this.Bottom;
            this.labeledStates.Add(label, state);
        }
        return state;
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
        // We continue with the looping, run body
        this.VisitExpression(node.Then);
        // Loop back to continue
        this.LoopTail(node.ContinueLabel);
    }

    public override void VisitLabelStatement(BoundLabelStatement node)
    {
        // Look up the previously saved label state
        var state = this.GetLabelState(node.Label);
        // Join in
        this.Join(ref this.State, in state);
        // Save a copy of this new state for the label
        this.labeledStates[node.Label] = this.Clone(this.State);
    }

    public override void VisitGotoExpression(BoundGotoExpression node)
    {
        // We join in with the referenced label
        var state = this.GetLabeledState(node.Target);
        this.Join(ref this.State, in state);
        this.labeledStates[node.Target] = this.State;
        // Below that the state is unreachable, detach
        this.State = this.Bottom;
    }

    public override void VisitAndExpression(BoundAndExpression node)
    {
        // TODO
        throw new NotImplementedException();
    }

    public override void VisitOrExpression(BoundOrExpression node)
    {
        // TODO
        throw new NotImplementedException();
    }

    public override void VisitRelationalExpression(BoundRelationalExpression node)
    {
        // TODO
        throw new NotImplementedException();
    }

    public override void VisitReturnExpression(BoundReturnExpression node)
    {
        // Detach
        this.State = this.Bottom;
    }
}
