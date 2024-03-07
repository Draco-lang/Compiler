using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// A constraint representing that a type needs to have a given member.
/// </summary>
/// <param name="Locator">The locator of the constraint.</param>
/// <param name="Receiver">The accessed symbol type.</param>
/// <param name="MemberName">The name of the member.</param>
/// <param name="MemberType">The type of the member.</param>
internal sealed record class Member(
    ConstraintLocator? Locator,
    TypeSymbol Receiver,
    string MemberName,
    TypeSymbol MemberType) : ConstraintBase(Locator)
{
    /// <summary>
    /// The completion source for the resolved member symbol.
    /// </summary>
    public SolverTaskCompletionSource<Symbol> CompletionSource { get; } = new();
}
