using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.OverloadResolution;

/// <summary>
/// A set of overload candidates.
/// </summary>
internal readonly struct OverloadCandidateSet : IReadOnlyCollection<CallCandidate<FunctionSymbol>>
{
    /// <summary>
    /// Constructs a new set of overload candidates.
    /// </summary>
    /// <param name="candidates">The candidate functions.</param>
    /// <param name="arguments">The arguments the candidates are called with.</param>
    /// <returns>The constructed candidate set.</returns>
    public static OverloadCandidateSet Create(
        IEnumerable<FunctionSymbol> candidates,
        IEnumerable<Argument> arguments)
    {
        var argList = arguments.ToImmutableArray();
        var initialCandidates = candidates.ToImmutableArray();
        var remainingCandidates = initialCandidates
            .Where(c => CallUtilities.MatchesParameterCount(c, argList.Length))
            .Select(CallCandidate.Create)
            .ToList();
        return new(initialCandidates, remainingCandidates, argList);
    }

    /// <summary>
    /// The arguments the candidates were called with.
    /// </summary>
    public ImmutableArray<Argument> Arguments { get; }

    /// <summary>
    /// The initial candidates the set was constructed with.
    /// </summary>
    public ImmutableArray<FunctionSymbol> InitialCandidates { get; }

    /// <summary>
    /// The remaining candidates.
    /// </summary>
    public IEnumerable<CallCandidate<FunctionSymbol>> Candidates => this.candidates;

    /// <summary>
    /// True, if the set is well defined, meaning that there is no need to further refine the candidates.
    /// This can mean that there is only one candidate left, no candidates are left, or even
    /// that the remaining candidates are well defined, meaning ambiguity.
    /// </summary>
    public bool IsWellDefined => this.Count <= 1
                              || this.candidates.All(c => c.IsWellDefined);

    /// <summary>
    /// Returns the dominating - best matching - candidates.
    /// </summary>
    public ImmutableArray<CallCandidate<FunctionSymbol>> Dominators =>
        CallScore.FindDominatorsBy(this.candidates, c => c.Score);

    public int Count => this.candidates.Count;

    private readonly List<CallCandidate<FunctionSymbol>> candidates;

    private OverloadCandidateSet(
        ImmutableArray<FunctionSymbol> initialCandidates,
        List<CallCandidate<FunctionSymbol>> candidates,
        ImmutableArray<Argument> arguments)
    {
        this.InitialCandidates = initialCandidates;
        this.Arguments = arguments;
        this.candidates = candidates;
    }

    /// <summary>
    /// Refines the candidates by scoring the arguments and eliminating any invalid candidates.
    /// </summary>
    /// <returns>True, if the refinement changed the set of candidates in some way.</returns>
    public bool Refine()
    {
        var changed = false;
        while (this.RefineOnce()) changed = true;
        return changed;
    }

    private bool RefineOnce()
    {
        var changed = false;
        // Iterate through all candidates
        for (var i = 0; i < this.Count;)
        {
            var candidate = this.candidates[i];

            // Compute any undefined arguments
            changed = candidate.Refine(this.Arguments) || changed;

            // Remove eliminated candidates
            if (candidate.IsEliminated)
            {
                this.candidates.RemoveAt(i);
                changed = true;
            }
            else
            {
                // Otherwise it stays
                ++i;
            }
        }
        return changed;
    }

    public IEnumerator<CallCandidate<FunctionSymbol>> GetEnumerator() => this.candidates.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
