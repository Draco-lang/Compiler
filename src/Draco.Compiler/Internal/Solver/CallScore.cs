using System;
using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents the comparison result of two <see cref="CallScore"/>s.
/// </summary>
internal enum CallScoreComparison
{
    /// <summary>
    /// The relationship could not be determined, because the scores are not well-defined.
    /// </summary>
    Undetermined,

    /// <summary>
    /// The first score vector dominates the second.
    /// </summary>
    FirstDominates,

    /// <summary>
    /// The second score vector dominates the first.
    /// </summary>
    SecondDominates,

    /// <summary>
    /// There is mutual non-dominance.
    /// </summary>
    NoDominance,

    /// <summary>
    /// The two scores are equal.
    /// </summary>
    Equal,
}

/// <summary>
/// Represents the score-vector for a single call.
/// </summary>
/// <param name="Scores">The array of scores for the arguments.</param>
internal readonly struct CallScore
{
    /// <summary>
    /// True, if the score vector has a zero element.
    /// </summary>
    public bool HasZero => this.scores.Contains(0);

    /// <summary>
    /// True, if the score vector is well-defined, meaning that there as no null scores.
    /// </summary>
    public bool IsWellDefined => !this.scores.Contains(null);

    /// <summary>
    /// The length of this vector.
    /// </summary>
    public int Length => this.scores.Length;

    /// <summary>
    /// The scores in this score vector.
    /// </summary>
    public int? this[int index]
    {
        get => this.scores[index];
        set
        {
            if (this.scores[index] is not null) throw new InvalidOperationException("can not modify non-null score");
            this.scores[index] = value;
        }
    }

    private readonly int?[] scores;

    public CallScore(int length)
    {
        this.scores = new int?[length];
    }

    /// <summary>
    /// Compares two call-scores.
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

            if (firstScore is null || secondScore is null) return CallScoreComparison.Undetermined;

            var scoreComparison = firstScore.Value.CompareTo(secondScore.Value);
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
    /// Finds the best call score in a sequence. The result heavily depends on the order, in case there is mutual
    /// non-dominance.
    /// </summary>
    /// <param name="scores">The scores to find the best in.</param>
    /// <returns>The best score, or null if can't be determined because of an empty sequence or non-well-defined
    /// score vectors.</returns>
    public static CallScore? FindBest(IEnumerable<CallScore> scores)
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
}
