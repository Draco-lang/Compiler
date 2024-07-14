using System.Collections.Immutable;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint for calling a single overload from a set of candidate functions.
/// </summary>
internal sealed class OverloadConstraint(
    string name,
    ImmutableArray<FunctionSymbol> candidates,
    ImmutableArray<ConstraintSolver.Argument> arguments,
    TypeSymbol returnType,
    ConstraintLocator locator) : Constraint<FunctionSymbol>(locator)
{
    private readonly record struct Candidate(FunctionSymbol Symbol, CallScore Score);

    /// <summary>
    /// The function name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The candidate functions to search among.
    /// </summary>
    public ImmutableArray<FunctionSymbol> Candidates { get; } = candidates;

    /// <summary>
    /// The arguments the function was called with.
    /// </summary>
    public ImmutableArray<ConstraintSolver.Argument> Arguments { get; } = arguments;

    /// <summary>
    /// The return type of the call.
    /// </summary>
    public TypeSymbol ReturnType { get; } = returnType;

    public override string ToString() =>
        $"Overload(candidates: [{string.Join(", ", this.Candidates)}], args: [{string.Join(", ", this.Arguments)}]) => {this.ReturnType}";
}
