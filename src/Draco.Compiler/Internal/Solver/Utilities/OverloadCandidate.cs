using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Utilities;

/// <summary>
/// Represents a single scored candidate for an overload.
/// </summary>
/// <param name="Symbol">The considered candidate.</param>
/// <param name="Score">The scoring it received.</param>
internal readonly record struct OverloadCandidate(FunctionSymbol Symbol, CallScore Score)
{
    /// <summary>
    /// Filters a list of candidates to only include those that dominate all others.
    /// </summary>
    /// <param name="candidates">The candidates to filter.</param>
    /// <returns>The list of candidates that dominate all others.</returns>
    public static ImmutableArray<OverloadCandidate> FindDominating(IReadOnlyList<OverloadCandidate> candidates)
    {
        if (candidates.Count == 0) return ImmutableArray<OverloadCandidate>.Empty;
        if (candidates.Count == 1) return [candidates[0]];

        // We have more than one, find the max dominator
        // NOTE: This might not be the actual dominator in case of mutual non-dominance
        var bestScore = CallScore.FindBest(candidates.Select(c => c.Score));
        // We keep every candidate that dominates this score, or there is mutual non-dominance
        var dominatingCandidates = candidates
            .Where(pair => bestScore is null
                        || CallScore.Compare(bestScore.Value, pair.Score)
                               is CallScoreComparison.Equal
                               or CallScoreComparison.NoDominance)
            .ToImmutableArray();
        Debug.Assert(dominatingCandidates.Length > 0);
        return dominatingCandidates;
    }
}
