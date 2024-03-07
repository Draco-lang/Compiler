using System.Collections.Immutable;
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
    ImmutableArray<ConstraintSolver.Argument> Arguments,
    TypeSymbol ReturnType) : ConstraintBase(Locator);
