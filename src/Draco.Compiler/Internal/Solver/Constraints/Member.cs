using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// A constraint representing that a type needs to have a given member.
/// </summary>
/// <param name="locator">The locator of the constraint.</param>
/// <param name="receiver">The accessed type.</param>
/// <param name="memberName">The name of the member.</param>
/// <param name="memberType">The type of the member.</param>
internal sealed class Member(
    ConstraintLocator? locator,
    TypeSymbol receiver,
    string memberName,
    TypeSymbol memberType) : Constraint(locator, SymbolResolutionErrors.MemberNotFound)
{
    /// <summary>
    /// The completion source for the resolved member symbol.
    /// </summary>
    public SolverTaskCompletionSource<Symbol> CompletionSource { get; } = new();

    /// <summary>
    /// The accessed type.
    /// </summary>
    public TypeSymbol Receiver { get; } = receiver;

    /// <summary>
    /// The name of the member.
    /// </summary>
    public string MemberName { get; } = memberName;

    /// <summary>
    /// The type of the member.
    /// </summary>
    public TypeSymbol MemberType { get; } = memberType;
}
