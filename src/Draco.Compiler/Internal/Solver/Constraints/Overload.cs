using System.Collections.Immutable;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Solver.Utilities;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// A constraint for calling a single overload from a set of candidate functions.
/// </summary>
/// <param name="Locator">The locator of the constraint.</param>
/// <param name="Name">The function name.</param>
/// <param name="Candidates">The candidate functions to search among.</param>
/// <param name="Arguments">The arguments the function was called with.</param>
/// <param name="ReturnType">The return type of the call.</param>
internal sealed record class Overload(
    ConstraintLocator? Locator,
    string Name,
    ImmutableArray<FunctionSymbol> Candidates,
    ImmutableArray<ArgumentDescription> Arguments,
    TypeSymbol ReturnType) : ConstraintBase(Locator)
{
    /// <summary>
    /// The completion source for the resolved overload symbol.
    /// </summary>
    public SolverTaskCompletionSource<FunctionSymbol> CompletionSource { get; } = new();

    public override string ToString() => base.ToString();
}
