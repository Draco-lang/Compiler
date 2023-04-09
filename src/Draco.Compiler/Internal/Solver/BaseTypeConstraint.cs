using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Constraint that enforces literal to have certain base type.
/// </summary>
internal sealed class BaseTypeConstraint : Constraint
{
    /// <summary>
    /// The <see cref="TypeVariable"/> to substitute.
    /// </summary>
    public TypeVariable Variable { get; }

    /// <summary>
    /// The base type that the <see cref="TypeVariable"/> should have.
    /// </summary>
    public Type BaseType { get; }

    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public ConstraintPromise<TypeVariable> Promise { get; }

    public BaseTypeConstraint(TypeVariable variable, Type baseType)
    {
        this.Variable = variable;
        this.BaseType = baseType;
        this.Promise = ConstraintPromise.FromResult(this, variable);
    }
}
