using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Constraint asserting that one type is assignable to another type.
/// </summary>
internal sealed class AssignableConstraint(
    TypeSymbol targetType,
    TypeSymbol assignedType,
    ConstraintLocator locator) : Constraint<Unit>(locator)
{
    /// <summary>
    /// The type being assigned to.
    /// </summary>
    public TypeSymbol TargetType { get; } = targetType;

    /// <summary>
    /// The type assigned.
    /// </summary>
    public TypeSymbol AssignedType { get; } = assignedType;

    public override string ToString() => $"Assign({this.TargetType}, {this.AssignedType})";
}
