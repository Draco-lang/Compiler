using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// Constraint asserting that one type is assignable to another type.
/// </summary>
/// <param name="locator">The locator of the constraint.</param>
/// <param name="targetType">The type being assigned to.</param>
/// <param name="assignedType">The type assigned.</param>
internal sealed class Assignable(
    ConstraintLocator? locator,
    TypeSymbol targetType,
    TypeSymbol assignedType) : Constraint(locator, TypeCheckingErrors.TypeMismatch)
{
    /// <summary>
    /// The type being assigned to.
    /// </summary>
    public TypeSymbol TargetType { get; } = targetType;

    /// <summary>
    /// The type assigned.
    /// </summary>
    public TypeSymbol AssignedType { get; } = assignedType;
}
