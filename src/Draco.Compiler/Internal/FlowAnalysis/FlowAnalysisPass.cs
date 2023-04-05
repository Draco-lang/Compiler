using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A base class for implementing dataflow passes.
/// </summary>
/// <typeparam name="TState">The state being tracked by the pass.</typeparam>
internal abstract class FlowAnalysisPass<TState>
{
    /// <summary>
    /// The lattice used by this pass.
    /// </summary>
    protected ILattice<TState> Lattice { get; }

    protected FlowAnalysisPass(ILattice<TState> lattice)
    {
        this.Lattice = lattice;
    }
}
