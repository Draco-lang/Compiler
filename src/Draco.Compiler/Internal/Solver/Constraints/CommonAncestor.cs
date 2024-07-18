using System.Collections.Immutable;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// A constraint for expressing that a set of types have some common ancestor.
/// </summary>
/// <param name="locator">The locator of the constraint.</param>
/// <param name="commonType">The common type of the <paramref name="alternativeTypes"/>.</param>
/// <param name="alternativeTypes">The alternative types to find the <paramref name="commonType"/> of.</param>
internal sealed class CommonAncestor(
    ConstraintLocator? locator,
    TypeSymbol commonType,
    ImmutableArray<TypeSymbol> alternativeTypes) : Constraint(locator, TypeCheckingErrors.NoCommonType)
{
    /// <summary>
    /// The common type of the <see cref="AlternativeTypes"/>.
    /// </summary>
    public TypeSymbol CommonType { get; } = commonType;

    /// <summary>
    /// The alternative types to find the <see cref="CommonType"/> of.
    /// </summary>
    public ImmutableArray<TypeSymbol> AlternativeTypes { get; } = alternativeTypes;
}
