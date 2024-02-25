using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a constraint for the solver.
/// </summary>
internal interface IConstraint
{
    /// <summary>
    /// The locator for the constraint.
    /// </summary>
    public ConstraintLocator Locator { get; }

    /// <summary>
    /// True, if this constraint should not report an error in case of failure.
    /// </summary>
    public bool Silent { get; }
}

/// <summary>
/// An <see cref="IConstraint"/> with known resolution type.
/// </summary>
/// <typeparam name="TResult">The result type of this constraint.</typeparam>
internal interface IConstraint<TResult> : IConstraint
{
    /// <summary>
    /// The completion source of this constraint.
    /// </summary>
    public SolverTaskCompletionSource<TResult> CompletionSource { get; }
}
