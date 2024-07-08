using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint representing that a type needs to have a given member.
/// </summary>
internal sealed class MemberConstraint(
    TypeSymbol accessed,
    string memberName,
    TypeSymbol memberType,
    bool silent,
    ConstraintLocator locator) : Constraint<Symbol>(locator)
{
    /// <summary>
    /// The accessed symbol type.
    /// </summary>
    public TypeSymbol Accessed { get; } = accessed;

    /// <summary>
    /// The name of the member.
    /// </summary>
    public string MemberName { get; } = memberName;

    /// <summary>
    /// The type of the member.
    /// </summary>
    public TypeSymbol MemberType { get; } = memberType;

    public override bool Silent { get; } = silent;

    public override string ToString() => $"Member({this.Accessed}, {this.MemberName})";
}
