using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a constraint that enforces two types to have the same base type.
/// </summary>
internal sealed class CommonBaseConstraint : Constraint
{
    /// <summary>
    /// The first type that must have the same base as <see cref="Second"/>.
    /// </summary>
    public TypeSymbol First { get; }

    /// <summary>
    /// The second type that must have the same base as <see cref="First"/>.
    /// </summary>
    public TypeSymbol Second { get; }

    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public ConstraintPromise<TypeSymbol> Promise { get; }

    public CommonBaseConstraint(TypeSymbol first, TypeSymbol second)
    {
        this.First = first;
        this.Second = second;
        this.Promise = ConstraintPromise.FromResult(this, first);
    }
}
