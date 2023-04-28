using System;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents the promise for a solver constraint. The promise can resolve, which means that the corresponding
/// constraint was solved successfully, or it can fail, which corresponds to the fact that the constraint can
/// not be solved.
/// </summary>
internal interface IConstraintPromise
{
    /// <summary>
    /// The constraint this promise belongs to.
    /// </summary>
    public IConstraint Constraint { get; }

    /// <summary>
    /// True, if this promise is resolved, either ba succeeding or failing.
    /// </summary>
    public bool IsResolved { get; }

    /// <summary>
    /// Configures the diagnostic messages for the constraint of this promise in case it fails.
    /// </summary>
    /// <param name="configure">The configuration function.</param>
    /// <returns>The promise instance.</returns>
    public IConstraintPromise ConfigureDiagnostic(Action<Diagnostic.Builder> configure);
}

/// <summary>
/// An <see cref="IConstraintPromise"/> with a known result type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The result type of the promise.</typeparam>
internal interface IConstraintPromise<TResult> : IConstraintPromise
{
    /// <summary>
    /// See <see cref="IConstraintPromise.Constraint"/>.
    /// </summary>
    public new IConstraint<TResult> Constraint { get; }

    /// <summary>
    /// The result of the promise.
    /// </summary>
    public TResult Result { get; }

    /// <summary>
    /// Resolves this promise with the given result.
    /// </summary>
    /// <param name="result">The result value to resolve with.</param>
    public void Resolve(TResult result);

    /// <summary>
    /// Fails this constraint, reporting the error.
    /// </summary>
    /// <param name="result">The result for the failure.</param>
    /// <param name="diagnostics">The diagnostics to report to, if needed.</param>
    public void Fail(TResult result, DiagnosticBag? diagnostics);

    /// <summary>
    /// <see cref="IConstraintPromise.ConfigureDiagnostic(Action{Diagnostic.Builder})"/>.
    /// </summary>
    public new IConstraintPromise<TResult> ConfigureDiagnostic(Action<Diagnostic.Builder> configure);
}
