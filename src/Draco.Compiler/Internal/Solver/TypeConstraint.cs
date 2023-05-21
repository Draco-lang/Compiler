using Draco.Compiler.Internal.Diagnostics;
using System.Collections.Generic;
using System;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a constraint that wait until <see cref="Type"/> is substituted.
/// </summary>
internal class TypeConstraint<TResult> : Constraint<IConstraintPromise<TResult>>
{
    /// <summary>
    /// The mapping function that executes user code once <see cref="Type"/> is substituted.
    /// </summary>
    Func<TypeSymbol, IConstraintPromise<TResult>> Map { get; }

    /// <summary>
    /// The <see cref="TypeSymbol"/> that is being waited for.
    /// </summary>
    public TypeSymbol Type { get; }

    public TypeConstraint(
        ConstraintSolver solver,
        TypeSymbol type,
        Func<TypeSymbol, IConstraintPromise<TResult>> map)
        : base(solver)
    {
        this.Type = type;
        this.Map = map;
    }

    public override string ToString() => $"Type({this.Type})";

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
        var type = this.Type;
        // Wait until resolved
        while (type.IsTypeVariable)
        {
            yield return SolveState.Stale;
            type = this.Unwrap(this.Type);
        }
        // We can resolve the awaited promise
        var mapped = this.Map(type);

        // Resolve this promise
        this.Promise.Resolve(mapped);
        yield return SolveState.Solved;
    }
}
