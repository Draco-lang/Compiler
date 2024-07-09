using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint asserting that two types have to be exactly the same.
/// </summary>
internal sealed class SameTypeConstraint(
    ImmutableArray<TypeSymbol> types,
    ConstraintLocator locator) : Constraint<Unit>(locator)
{
    /// <summary>
    /// The types that all should be the same.
    /// </summary>
    public ImmutableArray<TypeSymbol> Types { get; } = types;

    public override string ToString() => $"SameType({string.Join(", ", this.Types)})";
}
