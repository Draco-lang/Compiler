using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint, that runs when another constraint has finished.
/// </summary>
/// <typeparam name="TAwaitedResult">The result of the awaited constraint.</typeparam>
/// <typeparam name="TResult">The result of this constraint.</typeparam>
internal sealed class AwaitConstraint<TAwaitedResult, TResult> : Constraint<TResult>
{
    /// <summary>
    /// The awaited constraint.
    /// </summary>
    public IConstraint<TAwaitedResult> Awaited { get; }

    /// <summary>
    /// The mapping function that transforms the result of <see cref="Awaited"/> to the new promise.
    /// </summary>
    public Func<TAwaitedResult, IConstraintPromise<TResult>> Map { get; }

    public AwaitConstraint(
        ConstraintSolver solver,
        IConstraint<TAwaitedResult> awaited,
        Func<TAwaitedResult, IConstraintPromise<TResult>> map)
        : base(solver)
    {
        this.Awaited = awaited;
        this.Map = map;
    }

    public override string ToString() => $"Await({this.Awaited})";

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
        // Wait until resolved
        while (!this.Awaited.Promise.IsResolved) yield return SolveState.Stale;

        // We can resolve the awaited promise
        var awaitedResult = this.Awaited.Promise.Result;
        var mappedPromise = this.Map(awaitedResult);

        // Now we can wait for the new promise
        while (!mappedPromise.IsResolved) yield return SolveState.Stale;

        // Solved
        this.Promise.Resolve(mappedPromise.Result);
        yield return SolveState.Solved;
    }
}
