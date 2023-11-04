using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint for expressing that a set of types have some common ancestor.
/// </summary>
internal sealed class CommonTypeConstraint : Constraint<Unit>
{
    /// <summary>
    /// The common type of the <see cref="AlternativeTypes"/>.
    /// </summary>
    public TypeSymbol CommonType { get; }

    /// <summary>
    /// The alternative types to find the <see cref="CommonType"/> of.
    /// </summary>
    public ImmutableArray<TypeSymbol> AlternativeTypes { get; }

    public CommonTypeConstraint(
        ConstraintSolver solver,
        TypeSymbol commonType,
        ImmutableArray<TypeSymbol> alternativeTypes,
        ConstraintLocator locator)
        : base(solver, locator)
    {
        this.CommonType = commonType;
        this.AlternativeTypes = alternativeTypes;
    }

    public override string ToString() => $"CommonType({string.Join(", ", this.AlternativeTypes)}) => {this.CommonType}";
}
