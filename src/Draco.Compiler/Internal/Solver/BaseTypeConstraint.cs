using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

internal sealed class BaseTypeConstraint : Constraint
{
    public TypeVariable Variable { get; }

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
