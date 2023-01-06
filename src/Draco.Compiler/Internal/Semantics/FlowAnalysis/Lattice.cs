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
    /// Deep-copies a lattice element.
    /// </summary>
    /// <param name="element">The lattoce element to deep-copy.</param>
    /// <returns>A copy of <paramref name="element"/>.</returns>
    public TElement Clone(TElement element);

    // Meet function

    public void Meet(ref TElement result, TElement input);

    // Transfer functions

    public void Transfer(ref TElement element, Ast.Decl.Variable node);
    public void Transfer(ref TElement element, Ast.Expr.Return node);
    public void Transfer(ref TElement element, Ast.Expr.Call node);
    public void Transfer(ref TElement element, Ast.Expr.Index node);
    public void Transfer(ref TElement element, Ast.Expr.Unary node);
    public void Transfer(ref TElement element, Ast.Expr.Binary node);
    public void Transfer(ref TElement element, Ast.Expr.Relational node);
    public void Transfer(ref TElement element, Ast.Expr.Assign node);
    public void Transfer(ref TElement element, Ast.Expr.And node);
    public void Transfer(ref TElement element, Ast.Expr.Or node);
}

/// <summary>
/// Utility base class for lattices.
/// </summary>
/// <typeparam name="TElement">The element type of the lattices.</typeparam>
internal abstract class LatticeBase<TElement> : ILattice<TElement>
{
    public abstract FlowDirection Direction { get; }
    public abstract TElement Identity { get; }

    public abstract bool Equals(TElement x, TElement y);
    public abstract int GetHashCode(TElement obj);
    public abstract TElement Clone(TElement element);

    public abstract void Meet(ref TElement result, TElement input);

    public virtual void Transfer(ref TElement element, Ast.Decl.Variable node) { }
    public virtual void Transfer(ref TElement element, Ast.Expr.Return node) { }
    public virtual void Transfer(ref TElement element, Ast.Expr.Call node) { }
    public virtual void Transfer(ref TElement element, Ast.Expr.Index node) { }
    public virtual void Transfer(ref TElement element, Ast.Expr.Unary node) { }
    public virtual void Transfer(ref TElement element, Ast.Expr.Binary node) { }
    public virtual void Transfer(ref TElement element, Ast.Expr.Relational node) { }
    public virtual void Transfer(ref TElement element, Ast.Expr.Assign node) { }
    public virtual void Transfer(ref TElement element, Ast.Expr.And node) { }
    public virtual void Transfer(ref TElement element, Ast.Expr.Or node) { }
}
