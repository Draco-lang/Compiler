using System.Collections.Generic;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Utility base-class for constraints.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
internal abstract class Constraint<TResult> : IConstraint<TResult>
{
    public ConstraintSolver Solver { get; }
    public IConstraintPromise<TResult> Promise { get; }
    IConstraintPromise IConstraint.Promise => this.Promise;
    public Diagnostic.Builder Diagnostic { get; } = new();

    protected Constraint(ConstraintSolver solver)
    {
        this.Solver = solver;
        this.Promise = ConstraintPromise.Create(this);
    }

    protected Constraint(ConstraintSolver solver, IConstraintPromise<TResult> promise)
    {
        this.Solver = solver;
        this.Promise = promise;
    }

    public override abstract string ToString();
    public abstract IEnumerable<SolveState> Solve(DiagnosticBag diagnostics);

    public virtual void FailSilently() { }

    // Utils

    protected bool Unify(TypeSymbol first, TypeSymbol second) => this.Solver.Unify(first, second);
}
