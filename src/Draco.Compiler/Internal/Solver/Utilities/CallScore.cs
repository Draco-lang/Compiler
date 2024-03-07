using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Utilities;

/// <summary>
/// Represents the score-vector for a single call.
/// </summary>
/// <param name="Scores">The array of scores for the arguments.</param>
internal readonly struct CallScore
{
    private const int NullScore = -1;

    /// <summary>
    /// True, if the score vector has a zero element.
    /// </summary>
    public bool HasZero => this.scores.Contains(0);

    /// <summary>
    /// True, if the score vector is well-defined, meaning that there as no null scores.
    /// </summary>
    public bool IsWellDefined => !this.scores.Contains(NullScore);

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
            if (this.scores[index] != NullScore) throw new InvalidOperationException("can not modify non-null score");
            this.scores[index] = value;
        }
    }

    private readonly int[] scores;

    public CallScore(int length)
    {
        this.scores = new int[length];
        Array.Fill(this.scores, NullScore);
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

            if (firstScore == NullScore || secondScore == NullScore) return CallScoreComparison.Undetermined;

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

    /// <summary>
    /// Scores a sequence of variadic function call argument.
    /// </summary>
    /// <param name="param">The variadic function parameter.</param>
    /// <param name="argTypes">The passed in argument types.</param>
    /// <returns>The score of the match. Null, if it can not be decided yet.</returns>
    public static int? ScoreVariadicArguments(ParameterSymbol param, IEnumerable<TypeSymbol> argTypes)
    {
        if (!param.IsVariadic) throw new ArgumentException("the provided parameter is not variadic", nameof(param));
        if (!BinderFacts.TryGetVariadicElementType(param.Type, out var elementType)) return 0;

        return argTypes
            .Select(argType => ScoreArgument(elementType, argType))
            .Append(FullScore)
            .Select(s => s / 2)
            .Min();
    }

    /// <summary>
    /// Scores a function call argument.
    /// </summary>
    /// <param name="param">The function parameter.</param>
    /// <param name="argType">The passed in argument type.</param>
    /// <returns>The score of the match. Null, if can not be decided yet.</returns>
    public static int? ScoreArgument(ParameterSymbol param, TypeSymbol argType)
    {
        if (param.IsVariadic) throw new ArgumentException("the provided parameter variadic", nameof(param));
        return ScoreArgument(param.Type, argType);
    }

    private const int FullScore = 16;
    private const int HalfScore = 8;
    private const int ZeroScore = 0;

    private static int? ScoreArgument(TypeSymbol paramType, TypeSymbol argType)
    {
        paramType = paramType.Substitution;
        argType = argType.Substitution;

        // If either are still not ground types, we can't decide
        if (!paramType.IsGroundType || !argType.IsGroundType) return null;

        // Exact equality is max score
        if (SymbolEqualityComparer.Default.Equals(paramType, argType)) return FullScore;

        // Base type match is half score
        if (SymbolEqualityComparer.Default.IsBaseOf(paramType, argType)) return HalfScore;

        // TODO: Unspecified what happens for generics
        // For now we require an exact match and score is the lowest score among generic args
        if (paramType.IsGenericInstance && argType.IsGenericInstance)
        {
            var paramGenericDefinition = paramType.GenericDefinition!;
            var argGenericDefinition = argType.GenericDefinition!;

            if (!SymbolEqualityComparer.Default.Equals(paramGenericDefinition, argGenericDefinition)) return ZeroScore;

            Debug.Assert(paramType.GenericArguments.Length == argType.GenericArguments.Length);
            return paramType.GenericArguments
                .Zip(argType.GenericArguments)
                .Select(pair => ScoreArgument(pair.First, pair.Second))
                .Min();
        }

        // Type parameter match is half score
        if (paramType is TypeParameterSymbol) return HalfScore;

        // Otherwise, no match
        return ZeroScore;
    }
}
