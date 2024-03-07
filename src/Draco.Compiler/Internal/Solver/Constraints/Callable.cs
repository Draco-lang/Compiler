using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// Represents a callability constraint for indirect calls.
/// </summary>
/// <param name="Locator">The locator of the constraint.</param>
/// <param name="CalledType">The called expression type.</param>
/// <param name="Arguments">The arguments the function was called with.</param>
/// <param name="ReturnType">The return type of the call.</param>
internal sealed record class Callable(
    ConstraintLocator? Locator,
    TypeSymbol CalledType,
    ImmutableArray<ConstraintSolver.Argument> Arguments,
    TypeSymbol ReturnType) : ConstraintBase(Locator);
