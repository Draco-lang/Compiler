using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint for calling a single overload from a set of candidate functions.
/// </summary>
internal sealed class OverloadConstraint : Constraint<FunctionSymbol>
{
    private readonly record struct Candidate(FunctionSymbol Symbol, CallScore Score);

    /// <summary>
    /// The function name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The candidate functions to search among.
    /// </summary>
    public ImmutableArray<FunctionSymbol> Candidates { get; }

    /// <summary>
    /// The arguments the function was called with.
    /// </summary>
    public ImmutableArray<object> Arguments { get; }

    /// <summary>
    /// The return type of the call.
    /// </summary>
    public TypeSymbol ReturnType { get; }

    public OverloadConstraint(
        ConstraintSolver solver,
        string name,
        ImmutableArray<FunctionSymbol> candidates,
        ImmutableArray<object> arguments,
        TypeSymbol returnType,
        ConstraintLocator locator)
        : base(solver, locator)
    {
        this.Name = name;
        this.Candidates = candidates;
        this.Arguments = arguments;
        this.ReturnType = returnType;
    }

    public override string ToString() =>
        $"Overload(candidates: [{string.Join(", ", this.Candidates)}], args: [{string.Join(", ", this.Arguments)}]) => {this.ReturnType}";
}
