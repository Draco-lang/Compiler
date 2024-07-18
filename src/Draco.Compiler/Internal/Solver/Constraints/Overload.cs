using System.Collections.Immutable;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// A constraint for calling a single overload from a set of candidate functions.
/// </summary>
/// <param name="locator">The locator of the constraint.</param>
/// <param name="functionName">The function name.</param>
/// <param name="candidates">The candidate functions to search among.</param>
/// <param name="returnType">The return type of the call.</param>
internal sealed class Overload(
    ConstraintLocator? locator,
    string functionName,
    OverloadCandidateSet candidates,
    TypeSymbol returnType) : Constraint(locator)
{
    /// <summary>
    /// The completion source for the resolved overload symbol.
    /// </summary>
    public SolverTaskCompletionSource<FunctionSymbol> CompletionSource { get; } = new();

    /// <summary>
    /// The function name.
    /// </summary>
    public string FunctionName { get; } = functionName;

    /// <summary>
    /// The candidate functions to search among.
    /// </summary>
    public OverloadCandidateSet Candidates { get; } = candidates;

    /// <summary>
    /// The return type of the call.
    /// </summary>
    public TypeSymbol ReturnType { get; } = returnType;
}
