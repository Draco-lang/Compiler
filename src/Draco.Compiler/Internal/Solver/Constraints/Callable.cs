using System.Collections.Immutable;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// Represents a callability constraint for indirect calls.
/// </summary>
/// <param name="locator">The locator of the constraint.</param>
/// <param name="calledType">The called expression type.</param>
/// <param name="arguments">The arguments the function was called with.</param>
/// <param name="returnType">The return type of the call.</param>
internal sealed class Callable(
    ConstraintLocator? locator,
    TypeSymbol calledType,
    ImmutableArray<Argument> arguments,
    TypeSymbol returnType) : Constraint(locator)
{
    /// <summary>
    /// The type of the called expression.
    /// </summary>
    public TypeSymbol CalledType { get; } = calledType;

    /// <summary>
    /// The arguments the function was called with.
    /// </summary>
    public ImmutableArray<Argument> Arguments { get; } = arguments;

    /// <summary>
    /// The return type of the call.
    /// </summary>
    public TypeSymbol ReturnType { get; } = returnType;
}
