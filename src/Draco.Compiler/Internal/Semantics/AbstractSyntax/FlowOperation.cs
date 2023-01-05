using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Semantics.Types;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// A continuous sequence of <see cref="FlowOperation"/>s.
/// </summary>
internal sealed class BasicBlock
{
    // NOTE: This is mutable for simplicity (of building), but analysis will take the readonly interfaces anyway

    public IList<FlowOperation> Operations { get; } = new List<FlowOperation>();
    public FlowControlOperation? Control { get; set; }

    public IEnumerable<BasicBlock> Successors => this.Control?.AllTargets ?? Enumerable.Empty<BasicBlock>();
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
        Ast Ast) : FlowOperation;

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
        Type.Function FunctionType,
        ImmutableArray<FlowOperation> Args) : FlowOperation;

    /// <summary>
    /// Storing some value.
    /// </summary>
    public sealed record class Assign(
        Ast Ast,
        ISymbol.IVariable Target,
        FlowOperation Value) : FlowOperation;

    /// <summary>
    /// A value potentially originating from multiple alternative branches.
    /// </summary>
    public sealed record class Phi(
        Ast Ast,
        ImmutableArray<KeyValuePair<BasicBlock, FlowOperation>> Sources) : FlowOperation;
}

/// <summary>
/// Any operation that manipulated control-flow.
/// </summary>
internal abstract record class FlowControlOperation
{
    /// <summary>
    /// All the possible places the control can jump to.
    /// </summary>
    public abstract IEnumerable<BasicBlock> AllTargets { get; }

    /// <summary>
    /// Conditional jump.
    /// </summary>
    public sealed record class IfElse(
        FlowOperation Condition,
        BasicBlock Then,
        BasicBlock Else) : FlowControlOperation
    {
        public override IEnumerable<BasicBlock> AllTargets
        {
            get
            {
                yield return this.Then;
                yield return this.Else;
            }
        }
    }

    /// <summary>
    /// Unconditional jump.
    /// </summary>
    public sealed record class Goto(
        BasicBlock Target) : FlowControlOperation
    {
        public override IEnumerable<BasicBlock> AllTargets
        {
            get
            {
                yield return this.Target;
            }
        }
    }

    /// <summary>
    /// Return from the method.
    /// </summary>
    public sealed record class Return(
        FlowOperation Value) : FlowControlOperation
    {
        public override IEnumerable<BasicBlock> AllTargets => Enumerable.Empty<BasicBlock>();
    }
}
