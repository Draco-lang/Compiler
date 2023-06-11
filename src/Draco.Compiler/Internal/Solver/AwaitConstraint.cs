using System;
using System.Collections.Generic;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint, that runs when another process has finished.
/// </summary>
/// <typeparam name="TResult">The result of this constraint.</typeparam>
internal sealed class AwaitConstraint<TResult> : Constraint<TResult>
{
    /// <summary>
    /// When true, we execute <see cref="Map"/>.
    /// </summary>
    public Func<bool> Awaited { get; }

    /// <summary>
    /// The mapping function that runs when <see cref="Awaited"/> is true.
    /// </summary>
    public Func<TResult> Map { get; }

    public AwaitConstraint(
        ConstraintSolver solver,
        Func<bool> awaited,
        Func<TResult> map)
        : base(solver)
    {
        this.Awaited = awaited;
        this.Map = map;
    }

    public override string ToString() => $"Await({this.Awaited})";

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
        // Wait until resolved
        while (!this.Awaited()) yield return SolveState.Stale;

        // We can resolve the awaited promise
        var mappedValue = this.Map();

        // Resolve this promise
        this.Promise.Resolve(mappedValue);
        yield return SolveState.Solved;
    }
}
