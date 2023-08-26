using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a constraint for the solver.
/// </summary>
internal interface IConstraint
{
    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public IConstraintPromise Promise { get; }

    /// <summary>
    /// The builder for the <see cref="Api.Diagnostics.Diagnostic"/>.
    /// </summary>
    public Diagnostic.Builder Diagnostic { get; }

    /// <summary>
    /// The locator for the constraint.
    /// </summary>
    public ConstraintLocator Locator { get; }
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
