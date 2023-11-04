using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Constraint asserting that one type is assignable to another type.
/// </summary>
internal sealed class AssignableConstraint : Constraint<Unit>
{
    /// <summary>
    /// The type being assigned to.
    /// </summary>
    public TypeSymbol TargetType { get; }

    /// <summary>
    /// The type assigned.
    /// </summary>
    public TypeSymbol AssignedType { get; }

    public AssignableConstraint(
        ConstraintSolver solver,
        TypeSymbol targetType,
        TypeSymbol assignedType,
        ConstraintLocator locator)
        : base(solver, locator)
    {
        this.TargetType = targetType;
        this.AssignedType = assignedType;
    }

    public override string ToString() => $"Assign({this.TargetType}, {this.AssignedType})";
}
