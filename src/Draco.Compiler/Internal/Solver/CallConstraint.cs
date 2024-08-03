using System.Collections.Immutable;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a callability constraint for indirect calls.
/// </summary>
internal sealed class CallConstraint(
    TypeSymbol calledType,
    ImmutableArray<Argument> arguments,
    TypeSymbol returnType,
    ConstraintLocator locator) : Constraint<Unit>(locator)
{
    /// <summary>
    /// The called expression type.
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

    public override string ToString() =>
        $"Call(function: {this.CalledType}, args: [{string.Join(", ", this.Arguments)}]) => {this.ReturnType}";
}
