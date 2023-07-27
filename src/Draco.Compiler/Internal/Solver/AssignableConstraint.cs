using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Constraint asserting that one type is assignable to other type.
/// </summary>
internal class AssignableConstraint : Constraint<Unit>
{
    /// <summary>
    /// The type being assigned to.
    /// </summary>
    public TypeSymbol TargetType { get; }

    /// <summary>
    /// The types assigned.
    /// </summary>
    public TypeSymbol AssignedType { get; }

    public AssignableConstraint(TypeSymbol targetType, TypeSymbol assignedType)
    {
        this.TargetType = targetType;
        this.AssignedType = assignedType;
    }

    public override string ToString() => $"Assign({this.TargetType}, {this.AssignedType})";
}
