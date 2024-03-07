using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// Constraint asserting that one type is assignable to another type.
/// </summary>
/// <param name="Locator">The locator of the constraint.</param>
/// <param name="TargetType">The type being assigned to.</param>
/// <param name="AssignedType">The type assigned.</param>
internal sealed record class Assignable(
    ConstraintLocator? Locator,
    TypeSymbol TargetType,
    TypeSymbol AssignedType) : ConstraintBase(Locator);
