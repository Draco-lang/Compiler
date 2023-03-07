using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a promise to a <see cref="Constraint"/> being solved.
/// </summary>
/// <typeparam name="TResult">The result type of the promise.</typeparam>
internal sealed class ConstraintSolverPromise<TResult>
{
    /// <summary>
    /// The result of the promise.
    /// </summary>
    public TResult Result { get; }

    /// <summary>
    /// The constraint being solved.
    /// </summary>
    public Constraint Constraint { get; }

    /// <summary>
    /// The builder for a <see cref="Diagnostics.Diagnostic"/>.
    /// </summary>
    public Diagnostic.Builder Diagnostic => this.Constraint.Diagnostic;

    public ConstraintSolverPromise(TResult result, Constraint constraint)
    {
        this.Result = result;
        this.Constraint = constraint;
    }

    public ConstraintSolverPromise<TResult> ConfigureDiagnostic(Action<Diagnostic.Builder> configure)
    {
        configure(this.Diagnostic);
        return this;
    }
}
