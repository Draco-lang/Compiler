using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A base class for implementing dataflow passes.
/// </summary>
/// <typeparam name="TState">The state being tracked by the pass.</typeparam>
internal abstract class FlowAnalysisPass<TState> : BoundTreeVisitor
{
    /// <summary>
    /// The lattice used by this pass.
    /// </summary>
    protected ILattice<TState> Lattice { get; }

    // NOTE: This is a field for a reason, we pass refs to this
    /// <summary>
    /// The current state.
    /// </summary>
    protected TState State;

    protected FlowAnalysisPass(ILattice<TState> lattice)
    {
        this.Lattice = lattice;
        this.State = lattice.Top;
    }

    private bool Join(in TState other) => this.Lattice.Join(ref this.State, other);
    private bool Meet(in TState other) => this.Lattice.Meet(ref this.State, other);
    private TState CloneState() => this.Lattice.Clone(this.State);

    public override void VisitIfExpression(BoundIfExpression node)
    {
        // First, the condition always executes
        this.VisitExpression(node.Condition);
        // Then we have two alternatives, so we save
        var elseState = this.CloneState();
        // We run the 'then' alternative
        this.VisitExpression(node.Then);
        var thenState = this.State;
        // Then we run the 'else' alternative from after the condition
        this.State = elseState;
        this.VisitExpression(node.Else);
        // Finally, we merge
        this.Join(in thenState);
    }

    public override void VisitWhileExpression(BoundWhileExpression node)
    {
        // TODO: Continue block
        // TODO: Condition
        // TODO: Body
        // TODO: Break block
    }

    public override void VisitLabelStatement(BoundLabelStatement node)
    {
        // TODO: Join in?
    }

    public override void VisitGotoExpression(BoundGotoExpression node)
    {
        // TODO: Detach
    }
}
