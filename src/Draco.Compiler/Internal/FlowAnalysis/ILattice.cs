using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Represents a lattice type for flow analysis.
/// </summary>
/// <typeparam name="TState">The state type this lattice handles.</typeparam>
internal interface ILattice<TState>
{
    /// <summary>
    /// The top element of this lattice, which generally represents "reachable, but no information yet",
    /// which is usually the state of the starting point.
    /// </summary>
    public TState Top { get; }

    /// <summary>
    /// The bottom element of this lattice, usually representing "unreachable".
    /// </summary>
    public TState Bottom { get; }

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
    public bool Join(ref TState target, in TState other);

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
    public bool Meet(ref TState target, in TState other);
}
