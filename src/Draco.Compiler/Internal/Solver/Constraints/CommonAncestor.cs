using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// A constraint for expressing that a set of types have some common ancestor.
/// </summary>
/// <param name="Locator">The locator of the constraint.</param>
/// <param name="CommonType">The common type of the <see cref="AlternativeTypes"/>.</param>
/// <param name="AlternativeTypes">The alternative types to find the <see cref="CommonType"/> of.</param>
internal sealed record class CommonAncestor(
    ConstraintLocator? Locator,
    TypeSymbol CommonType,
    ImmutableArray<TypeSymbol> AlternativeTypes) : ConstraintBase(Locator);
