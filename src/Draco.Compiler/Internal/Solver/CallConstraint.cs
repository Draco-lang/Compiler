using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a callability constraint for indirect calls.
/// </summary>
internal sealed class CallConstraint : Constraint<Unit>
{
    /// <summary>
    /// The called expression type.
    /// </summary>
    public TypeSymbol CalledType { get; }

    /// <summary>
    /// The arguments the function was called with.
    /// </summary>
    public ImmutableArray<object> Arguments { get; }

    /// <summary>
    /// The return type of the call.
    /// </summary>
    public TypeSymbol ReturnType { get; }

    public CallConstraint(
        TypeSymbol calledType,
        ImmutableArray<object> arguments,
        TypeSymbol returnType,
        ConstraintLocator locator)
        : base(locator)
    {
        this.CalledType = calledType;
        this.Arguments = arguments;
        this.ReturnType = returnType;
    }

    public override string ToString() =>
        $"Call(function: {this.CalledType}, args: [{string.Join(", ", this.Arguments)}]) => {this.ReturnType}";
}
