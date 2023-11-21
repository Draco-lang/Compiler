using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private readonly record struct OverloadCandidate(FunctionSymbol Symbol, CallScore Score);

    private FunctionTypeSymbol MakeMismatchedFunctionType(ImmutableArray<Argument> args, TypeSymbol returnType) => new(
        args
            .Select(a => new SynthetizedParameterSymbol(null, a.Type))
            .Cast<ParameterSymbol>()
            .ToImmutableArray(),
        returnType);

    private static ImmutableArray<FunctionSymbol> GetDominatingCandidates(IReadOnlyList<OverloadCandidate> candidates)
    {
        // For a single candidate, don't bother
        if (candidates.Count == 1) return ImmutableArray.Create(candidates[0].Symbol);

        // We have more than one, find the max dominator
        // NOTE: This might not be the actual dominator in case of mutual non-dominance
        var bestScore = CallScore.FindBest(candidates.Select(c => c.Score));
        // We keep every candidate that dominates this score, or there is mutual non-dominance
        var dominatingCandidates = candidates
            .Where(pair => bestScore is null
                        || CallScore.Compare(bestScore.Value, pair.Score)
                               is CallScoreComparison.Equal
                               or CallScoreComparison.NoDominance)
            .Select(pair => pair.Symbol)
            .ToImmutableArray();
        Debug.Assert(dominatingCandidates.Length > 0);
        return dominatingCandidates;
    }

    private FunctionSymbol ChooseSymbol(FunctionSymbol chosen)
    {
        // Nongeneric, just return
        if (!chosen.IsGenericDefinition) return chosen;

        // Implicit generic instantiation
        // Create the proper number of type variables as type arguments
        var typeArgs = Enumerable
            .Range(0, chosen.GenericParameters.Length)
            .Select(_ => this.AllocateTypeVariable())
            .Cast<TypeSymbol>()
            .ToImmutableArray();

        // Instantiate the chosen symbol
        var instantiated = chosen.GenericInstantiate(chosen.ContainingSymbol, typeArgs);
        return instantiated;
    }

    private void UnifyParameterWithArgument(TypeSymbol paramType, Argument argument) => _ = this.Assignable(
        paramType,
        argument.Type,
        argument.Syntax is null ? ConstraintLocator.Null : ConstraintLocator.Syntax(argument.Syntax));

    private static bool MatchesParameterCount(FunctionSymbol function, int argc)
    {
        // Exact count match is always eligibe by only param count
        if (function.Parameters.Length == argc) return true;
        // If not variadic, we do need an exact match
        if (!function.IsVariadic) return false;
        // Otherise, there must be one less, exactly as many, or more arguments
        //  - one less means nullary variadics
        //  - exact match is one variadic
        //  - more is more variadics
        if (argc + 1 >= function.Parameters.Length) return true;
        // No match
        return false;
    }

    private bool RefineOverloadScores(
        List<OverloadCandidate> candidates,
        ImmutableArray<Argument> arguments,
        out bool wellDefined)
    {
        var changed = false;
        wellDefined = true;
        // Iterate through all candidates
        for (var i = 0; i < candidates.Count;)
        {
            var candidate = candidates[i];

            // Compute any undefined arguments
            changed = this.AdjustOverloadScore(candidate, arguments) || changed;
            // We consider having a 0-element well-defined, since we are throwing it away
            var hasZero = candidate.Score.HasZero;
            wellDefined = wellDefined && (candidate.Score.IsWellDefined || hasZero);

            // If any of the score vector components reached 0, we exclude the candidate
            if (hasZero)
            {
                candidates.RemoveAt(i);
            }
            else
            {
                // Otherwise it stays
                ++i;
            }
        }
        return changed;
    }

    private bool AdjustScore(FunctionTypeSymbol candidate, ImmutableArray<Argument> args, CallScore scoreVector)
    {
        Debug.Assert(candidate.Parameters.Length == args.Length);
        Debug.Assert(candidate.Parameters.Length == scoreVector.Length);

        var changed = false;
        for (var i = 0; i < scoreVector.Length; ++i)
        {
            var param = candidate.Parameters[i];
            var arg = args[i];
            var score = scoreVector[i];

            // If the argument is not null, it means we have already scored it
            if (score is not null) continue;

            score = ScoreArgument(param, arg.Type);
            changed = changed || score is not null;
            scoreVector[i] = score;

            // If the score hit 0, terminate early, this overload got eliminated
            if (score == 0) return changed;
        }
        return changed;
    }

    private bool AdjustOverloadScore(OverloadCandidate candidate, ImmutableArray<Argument> arguments)
    {
        var changed = false;
        var (func, scoreVector) = candidate;

        for (var i = 0; i < scoreVector.Length; ++i)
        {
            var param = func.Parameters[i];
            // Handle that separately
            if (param.IsVariadic) continue;

            if (arguments.Length == i)
            {
                // Special case, this call was extended because of variadics
                if (scoreVector[i] is null)
                {
                    scoreVector[i] = FullScore;
                    changed = true;
                }
                continue;
            }

            var argType = arguments[i].Type;
            var score = scoreVector[i];

            // If the argument is not null, it means we have already scored it
            if (score is not null) continue;

            score = ScoreArgument(param, argType);
            changed = changed || score is not null;
            scoreVector[i] = score;

            // If the score hit 0, terminate early, this overload got eliminated
            if (score == 0) return changed;
        }
        // Handle variadic arguments
        if (func.IsVariadic && scoreVector[^1] is null)
        {
            var variadicParam = func.Parameters[^1];
            var variadicArgTypes = arguments
                .Skip(func.Parameters.Length - 1)
                .Select(a => a.Type);
            var score = ScoreVariadicArguments(variadicParam, variadicArgTypes);
            changed = changed || score is not null;
            scoreVector[^1] = score;
        }
        return changed;
    }

    /// <summary>
    /// Scores a sequence of variadic function call argument.
    /// </summary>
    /// <param name="param">The variadic function parameter.</param>
    /// <param name="argTypes">The passed in argument types.</param>
    /// <returns>The score of the match.</returns>
    private static int? ScoreVariadicArguments(ParameterSymbol param, IEnumerable<TypeSymbol> argTypes)
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
    /// <returns>The score of the match.</returns>
    private static int? ScoreArgument(ParameterSymbol param, TypeSymbol argType)
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
