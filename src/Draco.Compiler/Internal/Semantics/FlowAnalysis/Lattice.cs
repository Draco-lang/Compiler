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
internal interface ILattice<TElement>
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

    public bool Meet(ref TElement result, TElement input);

    // Join functions

    public bool Join(ref TElement element, Ast.Decl.Variable node);
    public bool Join(ref TElement element, Ast.Expr.Return node);
    public bool Join(ref TElement element, Ast.Expr.Call node);
    public bool Join(ref TElement element, Ast.Expr.Index node);
    public bool Join(ref TElement element, Ast.Expr.Unary node);
    public bool Join(ref TElement element, Ast.Expr.Binary node);
    public bool Join(ref TElement element, Ast.Expr.Relational node);
    public bool Join(ref TElement element, Ast.Expr.Assign node);
    public bool Join(ref TElement element, Ast.Expr.And node);
    public bool Join(ref TElement element, Ast.Expr.Or node);
}

/// <summary>
/// Utility base class for lattices.
/// </summary>
/// <typeparam name="TElement">The element type of the lattices.</typeparam>
internal abstract class LatticeBase<TElement> : ILattice<TElement>
{
    public abstract FlowDirection Direction { get; }
    public abstract TElement Identity { get; }

    public abstract TElement Clone(TElement element);
    public abstract bool Meet(ref TElement result, TElement input);

    public virtual bool Join(ref TElement element, Ast.Decl.Variable node) => false;
    public virtual bool Join(ref TElement element, Ast.Expr.Return node) => false;
    public virtual bool Join(ref TElement element, Ast.Expr.Call node) => false;
    public virtual bool Join(ref TElement element, Ast.Expr.Index node) => false;
    public virtual bool Join(ref TElement element, Ast.Expr.Unary node) => false;
    public virtual bool Join(ref TElement element, Ast.Expr.Binary node) => false;
    public virtual bool Join(ref TElement element, Ast.Expr.Relational node) => false;
    public virtual bool Join(ref TElement element, Ast.Expr.Assign node) => false;
    public virtual bool Join(ref TElement element, Ast.Expr.And node) => false;
    public virtual bool Join(ref TElement element, Ast.Expr.Or node) => false;
}
