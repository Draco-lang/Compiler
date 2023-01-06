using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Represents a flow direction in DFA.
/// </summary>
internal enum DataFlowDirection
{
    /// <summary>
    /// The analysis goes forward.
    /// </summary>
    Forward,

    /// <summary>
    /// The analysis goes backward.
    /// </summary>
    Backward,
}

/// <summary>
/// Represents a lattice made up of a certain type of elements.
/// </summary>
/// <typeparam name="TElement">The element type of the lattices.</typeparam>
/// <typeparam name="TStatement">The statement type the lattice can handle.</typeparam>
internal interface ILattice<TElement, TStatement> : IEqualityComparer<TElement>
{
    /// <summary>
    /// The flow direction the lattice is defined for.
    /// </summary>
    public DataFlowDirection Direction { get; }

    /// <summary>
    /// The identity element of the lattice (also known as the top element).
    /// </summary>
    public TElement Identity { get; }

    /// <summary>
    /// Deep-clones the given lattice element.
    /// </summary>
    /// <param name="element">The element to clone.</param>
    /// <returns>The clone of <paramref name="element"/>.</returns>
    public TElement Clone(TElement element);

    /// <summary>
    /// Transfers the given lattice element through the given statement, according to <see cref="Direction"/>.
    /// </summary>
    /// <param name="element">The element to transfer.</param>
    /// <param name="statement">The statement to use for the transition.</param>
    public void Transfer(ref TElement element, TStatement statement);

    /// <summary>
    /// Joins up the lattice elements from multiple predecessors.
    /// </summary>
    /// <param name="inputs">The lattice elements to join.</param>
    /// <returns>The resulting lattice element.</returns>
    public TElement Meet(IEnumerable<TElement> inputs);

}
