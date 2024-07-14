using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint for calling a single overload from a set of candidate functions.
/// </summary>
internal sealed class OverloadConstraint(
    string name,
    OverloadCandidateSet candidateSet,
    TypeSymbol returnType,
    ConstraintLocator locator) : Constraint<FunctionSymbol>(locator)
{
    /// <summary>
    /// The function name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The set of candidates.
    /// </summary>
    public OverloadCandidateSet CandidateSet { get; } = candidateSet;

    /// <summary>
    /// The candidate functions to search among.
    /// </summary>
    public IEnumerable<FunctionSymbol> Candidates => this.CandidateSet.Select(c => c.Symbol);

    /// <summary>
    /// The arguments the function was called with.
    /// </summary>
    public ImmutableArray<Argument> Arguments => this.CandidateSet.Arguments;

    /// <summary>
    /// The return type of the call.
    /// </summary>
    public TypeSymbol ReturnType { get; } = returnType;

    public override string ToString() =>
        $"Overload(candidates: [{string.Join(", ", this.Candidates)}], args: [{string.Join(", ", this.Arguments)}]) => {this.ReturnType}";
}
