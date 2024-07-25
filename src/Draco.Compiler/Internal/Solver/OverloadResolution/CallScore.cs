using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Draco.Compiler.Internal.Solver.OverloadResolution;

/// <summary>
/// Represents the score-vector for a single call.
/// Note, that this type is mutable, refining the scores as more data is available.
/// </summary>
internal readonly struct CallScore(int length)
{
    private const int Undefined = ArgumentScore.Undefined;

    /// <summary>
    /// Compares two call-scores of the same length.
    /// </summary>
    /// <param name="first">The first score to compare.</param>
    /// <param name="second">The second score to compare.</param>
    /// <returns>The relationship between <paramref name="first"/> and <paramref name="second"/>.</returns>
    public static CallScoreComparison Compare(CallScore first, CallScore second)
    {
        if (first.Length != second.Length)
        {
            throw new InvalidOperationException("Can not compare call scores of different length");
        }

        var relation = CallScoreComparison.Equal;
        for (var i = 0; i < first.Length; ++i)
        {
            var firstScore = first[i];
            var secondScore = second[i];

            if (firstScore == Undefined || secondScore == Undefined) return CallScoreComparison.Undetermined;

            var scoreComparison = firstScore.CompareTo(secondScore);
            relation = relation switch
            {
                CallScoreComparison.FirstDominates when scoreComparison < 0 => CallScoreComparison.NoDominance,
                CallScoreComparison.SecondDominates when scoreComparison > 0 => CallScoreComparison.NoDominance,
                CallScoreComparison.Equal when scoreComparison < 0 => CallScoreComparison.SecondDominates,
                CallScoreComparison.Equal when scoreComparison > 0 => CallScoreComparison.FirstDominates,
                _ => relation,
            };

            if (relation == CallScoreComparison.NoDominance) return CallScoreComparison.NoDominance;
        }
        return relation;
    }

    /// <summary>
    /// Finds the dominating scores among a sequence of items.
    /// </summary>
    /// <typeparam name="T">THe type of items to find the dominators aming.</typeparam>
    /// <param name="items">The items to find the dominators among.</param>
    /// <param name="scoreSelector">The score selector function.</param>
    /// <returns>The dominating elements among <paramref name="items"/>.</returns>
    public static ImmutableArray<T> FindDominatorsBy<T>(
        IEnumerable<T> items,
        Func<T, CallScore> scoreSelector)
    {
        var candidates = items.ToImmutableArray();

        // Optimization, for a single or no candidate, don't bother
        if (candidates.Length <= 1) return candidates;

        // We have more than one, find the max dominator
        // NOTE: This might not be the actual dominator in case of mutual non-dominance
        var dominatingScore = FindDominatingScore(candidates.Select(scoreSelector));
        // We keep every candidate that dominates this score, or there is mutual non-dominance
        var dominatingCandidates = candidates
            .Where(candidate => dominatingScore is null
                             || Compare(dominatingScore.Value, scoreSelector(candidate))
                                    is CallScoreComparison.Equal
                                    or CallScoreComparison.NoDominance)
            .ToImmutableArray();

        Debug.Assert(dominatingCandidates.Length > 0);
        return dominatingCandidates;
    }

    /// <summary>
    /// Finds the best call score in a sequence. The result heavily depends on the order, in case there is mutual
    /// non-dominance.
    /// </summary>
    /// <param name="scores">The scores to find the best in.</param>
    /// <returns>The best score, or null if can't be determined because of an empty sequence or non-well-defined
    /// score vectors.</returns>
    private static CallScore? FindDominatingScore(IEnumerable<CallScore> scores)
    {
        var enumerator = scores.GetEnumerator();
        if (!enumerator.MoveNext()) return null;

        var best = enumerator.Current;
        while (enumerator.MoveNext())
        {
            var score = enumerator.Current;

            var cmp = Compare(best, score);
            if (cmp == CallScoreComparison.Undetermined) return null;
            if (cmp == CallScoreComparison.SecondDominates) best = score;
        }
        return best;
    }

    /// <summary>
    /// True, if the score vector has a zero element, in which case the call is a guaranteed no match.
    /// </summary>
    public bool HasZero => this.scores.Contains(0);

    /// <summary>
    /// True, if the score vector is well-defined, meaning that there as no undefined scores.
    /// </summary>
    public bool IsWellDefined => !this.scores.Contains(Undefined);

    /// <summary>
    /// The length of this vector.
    /// </summary>
    public int Length => this.scores.Length;

    /// <summary>
    /// The scores in this score vector.
    /// </summary>
    public int this[int index]
    {
        get => this.scores[index];
        set
        {
            if (index < 0 || index >= this.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (this.scores[index] != Undefined) throw new InvalidOperationException("can not modify non-null score");
            this.scores[index] = value;
        }
    }

    private readonly int[] scores = Enumerable.Repeat(Undefined, length).ToArray();
}
