using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint for expressing that a set of types have some common ancestor.
/// </summary>
internal sealed class CommonTypeConstraint(
    TypeSymbol commonType,
    ImmutableArray<TypeSymbol> alternativeTypes,
    ConstraintLocator locator) : Constraint<Unit>(locator)
{
    /// <summary>
    /// The common type of the <see cref="AlternativeTypes"/>.
    /// </summary>
    public TypeSymbol CommonType { get; } = commonType;

    /// <summary>
    /// The alternative types to find the <see cref="CommonType"/> of.
    /// </summary>
    public ImmutableArray<TypeSymbol> AlternativeTypes { get; } = alternativeTypes;

    public override string ToString() => $"CommonType({string.Join(", ", this.AlternativeTypes)}) => {this.CommonType}";
}
