using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a constraint for the solver.
/// </summary>
internal interface IConstraint
{
    /// <summary>
    /// The solver this constraint belongs to.
    /// </summary>
    public ConstraintSolver Solver { get; }

    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public IConstraintPromise Promise { get; }

    /// <summary>
    /// The builder for the <see cref="Api.Diagnostics.Diagnostic"/>.
    /// </summary>
    public Diagnostic.Builder Diagnostic { get; }

    /// <summary>
    /// The type-variables involved in this constraint.
    /// </summary>
    public IEnumerable<TypeVariable> TypeVariables { get; }

    /// <summary>
    /// Attempts to solve this constraint.
    /// </summary>
    /// <returns>The state that corresponds to how the constraint has progressed.</returns>
    public SolveState Solve();
}

/// <summary>
/// An <see cref="IConstraint"/> with known resolution type.
/// </summary>
/// <typeparam name="TResult">The result type of this constraint.</typeparam>
internal interface IConstraint<TResult> : IConstraint
{
    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public new IConstraintPromise<TResult> Promise { get; }
}
