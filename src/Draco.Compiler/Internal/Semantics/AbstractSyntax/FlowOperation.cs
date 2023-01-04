using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Internal.Semantics.Symbols;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// A continuous sequence of <see cref="FlowOperation"/>s.
/// </summary>
internal sealed record class BasicBlock(
    ImmutableArray<FlowOperation> Operations);

/// <summary>
/// A single operation in the <see cref="Ast"/> represented for flow analysis.
/// </summary>
internal abstract record class FlowOperation
{
    /// <summary>
    /// Any value deemed to be atomic or literal from flow-analysis perspective.
    /// </summary>
    public sealed record class Constant(
        Ast.Expr Value) : FlowOperation;

    /// <summary>
    /// A symbol reference.
    /// </summary>
    public sealed record class Reference(
        ISymbol.ITyped Symbol) : FlowOperation;

    /// <summary>
    /// Any call-like expression.
    /// </summary>
    public sealed record class Call(
        ISymbol.IFunction Function,
        ImmutableArray<FlowOperation> Args) : FlowOperation;

    /// <summary>
    /// Storing some value.
    /// </summary>
    public sealed record class Assign(
        ISymbol.IVariable Target,
        FlowOperation Value) : FlowOperation;
}

/// <summary>
/// Any operation that manipulated control-flow.
/// </summary>
internal abstract record class FlowControlOperation
{
    /// <summary>
    /// Conditional jump.
    /// </summary>
    public sealed record class IfElse(
        FlowOperation Condition,
        BasicBlock Then,
        BasicBlock Else) : FlowControlOperation;

    /// <summary>
    /// Unconditional jump.
    /// </summary>
    public sealed record class Goto(
        BasicBlock Target) : FlowControlOperation;

    /// <summary>
    /// Return from the method.
    /// </summary>
    public sealed record class Return(
        FlowOperation Value) : FlowControlOperation;
}
