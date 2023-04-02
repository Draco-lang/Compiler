using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a constraint that enforces two types to have the same base type.
/// </summary>
internal class CommonBaseConstraint : Constraint
{
    /// <summary>
    /// The first type that must have the same base as <see cref="Second"/>.
    /// </summary>
    public Type First { get; }

    /// <summary>
    /// The second type that must have the same base as <see cref="First"/>.
    /// </summary>
    public Type Second { get; }

    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public ConstraintPromise<Type> Promise { get; }

    public CommonBaseConstraint(Type first, Type second)
    {
        this.First = first;
        this.Second = second;
        this.Promise = ConstraintPromise.FromResult(this, first);
    }
}
