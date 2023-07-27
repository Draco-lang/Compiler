using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Constraint asserting that one type is base type of the other.
/// </summary>
internal class CommonTypeConstraint : Constraint<Unit>
{
    /// <summary>
    /// The common type of the <see cref="AlternativeTypes"/>.
    /// </summary>
    public TypeSymbol CommonType { get; }

    /// <summary>
    /// The alternative types to find the <see cref="CommonType"/> of.
    /// </summary>
    public ImmutableArray<TypeSymbol> AlternativeTypes { get; }

    public CommonTypeConstraint(TypeSymbol commonType, ImmutableArray<TypeSymbol> alternativeTypes)
    {
        this.CommonType = commonType;
        this.AlternativeTypes = alternativeTypes;
    }

    public override string ToString() => $"CommonType({this.CommonType}, {string.Join(", ", this.AlternativeTypes)})";
}
