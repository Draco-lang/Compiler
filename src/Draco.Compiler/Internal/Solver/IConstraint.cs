using Draco.Compiler.Internal.Binding.Tasks;

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
    public BindingTaskCompletionSource<TResult> CompletionSource { get; }
}
