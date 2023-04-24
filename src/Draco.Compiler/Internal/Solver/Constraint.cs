using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public abstract IEnumerable<TypeVariable> TypeVariables { get; }

    protected Constraint(ConstraintSolver solver)
    {
        this.Solver = solver;
        this.Promise = ConstraintPromise.Create<TResult>(this);
    }

    public abstract override string ToString();
    public abstract SolveState Solve(DiagnosticBag diagnostics);
}
