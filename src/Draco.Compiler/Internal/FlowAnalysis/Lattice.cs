using System.Collections.Generic;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Represents a flow direction in DFA.
/// </summary>
internal enum FlowDirection
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
internal interface ILattice<TElement> : IEqualityComparer<TElement>
{
    /// <summary>
    /// The flow direction the lattice is defined for.
    /// </summary>
    public FlowDirection Direction { get; }

    /// <summary>
    /// The identity element of the lattice (also known as the top element).
    /// </summary>
    public TElement Identity { get; }

    /// <summary>
    /// Merges two lattice elements from two alternative paths.
    /// </summary>
    /// <param name="a">The first lattice element to merge.</param>
    /// <param name="b">The second lattice element to merge.</param>
    /// <returns>The merged lattice element of <paramref name="a"/> and <paramref name="b"/>.</returns>
    public TElement Meet(TElement a, TElement b);

    /// <summary>
    /// Joins two lattice elements in a sequence.
    /// </summary>
    /// <param name="a">The first lattice element in the sequence.</param>
    /// <param name="b">The second lattice element in the sequence.</param>
    /// <returns>The merged lattice element that represents <paramref name="a"/> then <paramref name="b"/>.</returns>
    public TElement Join(TElement a, TElement b);

    /// <summary>
    /// Retrieves the lattice element that corresponds to transfering over <paramref name="node"/> from the identity element.
    /// </summary>
    /// <param name="node">The node that is being transfered through.</param>
    /// <returns>The lattice element that is produced by transfering <see cref="Identity"/> over <paramref name="node"/>.</returns>
    public TElement Transfer(BoundNode node);
}
