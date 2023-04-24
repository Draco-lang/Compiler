using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Factory methods for <see cref="IConstraintPromise"/>s.
/// </summary>
internal static class ConstraintPromise
{
    /// <summary>
    /// Constructs a constraint promise that is already resolved.
    /// </summary>
    /// <typeparam name="TResult">The result type of the promise.</typeparam>
    /// <param name="solver">The solver the result is constructed for.</param>
    /// <param name="result">The resolved value.</param>
    /// <returns>The constructed promise, containing <paramref name="result"/> as the result value.</returns>
    public static IConstraintPromise<TResult> FromResult<TResult>(ConstraintSolver solver, TResult result)
    {
        var constraint = new SolvedConstraint(solver);
        return new ResolvedConstraintPromise<TResult>(constraint, result);
    }

    /// <summary>
    /// Maps the result of the given constraint promise using a mapping function.
    /// </summary>
    /// <typeparam name="TOldResult">The result the promise originally returned.</typeparam>
    /// <typeparam name="TNewResult">The type the mapping function returns.</typeparam>
    /// <param name="promise">The promise to map.</param>
    /// <param name="func">The mapping function.</param>
    /// <returns>A new promise, that maps the result of <paramref name="promise"/> using <paramref name="func"/>.</returns>
    public static IConstraintPromise<TNewResult> Map<TOldResult, TNewResult>(
        this IConstraintPromise<TOldResult> promise,
        Func<TOldResult, TNewResult> func) =>
        // TODO
        throw new NotImplementedException();

    /// <summary>
    /// Binds a constraint promise to a new promise based on the old result, once the promise resolves.
    /// </summary>
    /// <typeparam name="TOldResult">The result of the old promise.</typeparam>
    /// <typeparam name="TNewResult">The result the mapped promise returns.</typeparam>
    /// <param name="promise">The constraint promise to bind.</param>
    /// <param name="func">A function transforming the result of <paramref name="promise"/> to the new constraint promise.</param>
    /// <returns>A new constraint promise, that only gets created, when <paramref name="promise"/> is resolved.</returns>
    public static IConstraintPromise<TNewResult> Bind<TOldResult, TNewResult>(
        this IConstraintPromise<TOldResult> promise,
        Func<TOldResult, IConstraintPromise<TNewResult>> func) =>
        // TODO
        throw new NotImplementedException();

    private sealed class SolvedConstraint : IConstraint
    {
        public ConstraintSolver Solver { get; }
        public Diagnostic.Builder Diagnostic { get; } = new();
        public IEnumerable<TypeVariable> TypeVariables => Enumerable.Empty<TypeVariable>();
        public SolveState Solve() => SolveState.Solved;

        public SolvedConstraint(ConstraintSolver solver)
        {
            this.Solver = solver;
        }
    }

    private sealed class ResolvedConstraintPromise<TResult> : IConstraintPromise<TResult>
    {
        public bool IsResolved => true;
        public IConstraint Constraint { get; }
        public TResult Result { get; }

        public ResolvedConstraintPromise(IConstraint constraint, TResult result)
        {
            this.Constraint = constraint;
            this.Result = result;
        }

        public void Resolve(TResult result) =>
            throw new InvalidOperationException("can not resolve an already solved constraint");
        public void Fail(TResult result, DiagnosticBag? diagnostics) =>
            throw new InvalidOperationException("can not resolve an already solved constraint");

        public IConstraintPromise<TResult> ConfigureDiagnostics(Action<Diagnostic.Builder> configure)
        {
            configure(this.Constraint.Diagnostic);
            return this;
        }
        IConstraintPromise IConstraintPromise.ConfigureDiagnostics(Action<Diagnostic.Builder> configure) =>
            this.ConfigureDiagnostics(configure);
    }
}
