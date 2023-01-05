using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Internal.Semantics.Symbols;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// A continuous sequence of <see cref="FlowOperation"/>s.
/// </summary>
internal sealed class BasicBlock
{
    // NOTE: This is mutable for simplicity (of building), but analysis will take the readonly interfaces anyway

    public IList<FlowOperation> Operations { get; } = new List<FlowOperation>();
    public FlowControlOperation? Control { get; set; }
}

/// <summary>
/// A single operation in the <see cref="Ast"/> represented for flow analysis.
/// </summary>
internal abstract record class FlowOperation
{
    public abstract Ast Ast { get; init; }

    /// <summary>
    /// Any value deemed to be atomic or literal from flow-analysis perspective.
    /// </summary>
    public sealed record class Constant(
        Ast Ast,
        Ast.Expr Value) : FlowOperation;

    /// <summary>
    /// A symbol reference.
    /// </summary>
    public sealed record class Reference(
        Ast Ast,
        ISymbol.ITyped Symbol) : FlowOperation;

    /// <summary>
    /// Any call-like expression.
    /// </summary>
    public sealed record class Call(
        Ast Ast,
        ISymbol.IFunction Function,
        ImmutableArray<FlowOperation> Args) : FlowOperation;

    /// <summary>
    /// Storing some value.
    /// </summary>
    public sealed record class Assign(
        Ast Ast,
        ISymbol.IVariable Target,
        FlowOperation Value) : FlowOperation;
}

/// <summary>
/// Any operation that manipulated control-flow.
/// </summary>
internal abstract record class FlowControlOperation
{
    public abstract Ast Ast { get; init; }

    /// <summary>
    /// Conditional jump.
    /// </summary>
    public sealed record class IfElse(
        Ast Ast,
        FlowOperation Condition,
        BasicBlock Then,
        BasicBlock Else) : FlowControlOperation;

    /// <summary>
    /// Unconditional jump.
    /// </summary>
    public sealed record class Goto(
        Ast Ast,
        BasicBlock Target) : FlowControlOperation;

    /// <summary>
    /// Return from the method.
    /// </summary>
    public sealed record class Return(
        Ast Ast,
        FlowOperation Value) : FlowControlOperation;
}
