using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Utilities;

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
    /// Deep-clones the given lattice element.
    /// </summary>
    /// <param name="element">The element to clone.</param>
    /// <returns>The clone of <paramref name="element"/>.</returns>
    public TElement Clone(TElement element);

    /// <summary>
    /// Joins up the lattice elements from multiple predecessors.
    /// </summary>
    /// <param name="inputs">The lattice elements to join.</param>
    /// <returns>The resulting lattice element.</returns>
    public TElement Meet(IEnumerable<TElement> inputs);

    // Join functions

    public TElement Join(ref TElement element, Ast.Decl.Variable node);
    public TElement Join(ref TElement element, Ast.Expr.Return node);
    public TElement Join(ref TElement element, Ast.Expr.Call node);
    public TElement Join(ref TElement element, Ast.Expr.Index node);
    public TElement Join(ref TElement element, Ast.Expr.Unary node);
    public TElement Join(ref TElement element, Ast.Expr.Binary node);
    public TElement Join(ref TElement element, Ast.Expr.Relational node);
    public TElement Join(ref TElement element, Ast.Expr.Assign node);
    public TElement Join(ref TElement element, Ast.Expr.And node);
    public TElement Join(ref TElement element, Ast.Expr.Or node);
}

internal abstract class LatticeBase<TElement> : ILattice<TElement>
{
    public abstract FlowDirection Direction { get; }
    public abstract TElement Identity { get; }

    public abstract TElement Clone(TElement element);
    public abstract bool Equals(TElement x, TElement y);
    public abstract int GetHashCode(TElement obj);

    public abstract TElement Meet(IEnumerable<TElement> inputs);

    public virtual TElement Join(ref TElement element, Ast.Decl.Variable node) => element;
    public virtual TElement Join(ref TElement element, Ast.Expr.Return node) => element;
    public virtual TElement Join(ref TElement element, Ast.Expr.Call node) => element;
    public virtual TElement Join(ref TElement element, Ast.Expr.Index node) => element;
    public virtual TElement Join(ref TElement element, Ast.Expr.Unary node) => element;
    public virtual TElement Join(ref TElement element, Ast.Expr.Binary node) => element;
    public virtual TElement Join(ref TElement element, Ast.Expr.Relational node) => element;
    public virtual TElement Join(ref TElement element, Ast.Expr.Assign node) => element;
    public virtual TElement Join(ref TElement element, Ast.Expr.And node) => element;
    public virtual TElement Join(ref TElement element, Ast.Expr.Or node) => element;
}
