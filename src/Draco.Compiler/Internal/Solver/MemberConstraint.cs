using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint representing that a type needs to have a given member.
/// </summary>
internal sealed class MemberConstraint : Constraint<Symbol>
{
    /// <summary>
    /// The accessed symbol type.
    /// </summary>
    public TypeSymbol Accessed { get; }

    /// <summary>
    /// The name of the member.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// The type of the member.
    /// </summary>
    public TypeSymbol MemberType { get; }

    public MemberConstraint(
        ConstraintSolver solver,
        TypeSymbol accessed,
        string memberName,
        TypeSymbol memberType,
        ConstraintLocator locator)
        : base(solver, locator)
    {
        this.Accessed = accessed;
        this.MemberName = memberName;
        this.MemberType = memberType;
    }

    public override string ToString() => $"Member({this.Accessed}, {this.MemberName})";
}
