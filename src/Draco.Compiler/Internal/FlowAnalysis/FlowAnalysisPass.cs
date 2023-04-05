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
}
