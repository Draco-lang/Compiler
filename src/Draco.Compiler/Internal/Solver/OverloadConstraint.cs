using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint for calling a single overload from a set of candidate functions.
/// </summary>
internal sealed class OverloadConstraint : Constraint<FunctionSymbol>
{
    private readonly record struct Candidate(FunctionSymbol Symbol, CallScore Score);

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
        ImmutableArray<FunctionSymbol> candidates,
        ImmutableArray<object> arguments,
        TypeSymbol returnType)
    {
        this.Candidates = candidates;
        this.Arguments = arguments;
        this.ReturnType = returnType;
    }

    public override string ToString() =>
        $"Overload(candidates: [{string.Join(", ", this.Candidates)}], args: [{string.Join(", ", this.Arguments)}]) => {this.ReturnType}";

    public override void FailSilently()
    {
        this.Unify(this.ReturnType, IntrinsicSymbols.ErrorType);
        var errorSymbol = new NoOverloadFunctionSymbol(this.Arguments.Length);
        this.Promise.Fail(errorSymbol, null);
    }

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
        var functionName = this.Candidates[0].Name;
        var functionsWithMatchingArgc = this.Candidates
            .Where(this.MatchesParameterCount)
            .ToList();
        var maxArgc = functionsWithMatchingArgc
            .Select(f => f.Parameters.Length)
            .Append(0)
            .Max();
        var candidates = functionsWithMatchingArgc
            .Select(f => new Candidate(f, new(maxArgc)))
            .ToList();

        while (true)
        {
            var changed = this.RefineScores(candidates, out var wellDefined);
            if (wellDefined) break;
            if (candidates.Count <= 1) break;
            yield return changed ? SolveState.AdvancedContinue : SolveState.Stale;
        }

        // We have all candidates well-defined, find the absolute dominator
        if (candidates.Count == 0)
        {
            this.Unify(this.ReturnType, IntrinsicSymbols.ErrorType);
            // Best-effort shape approximation
            var errorSymbol = new NoOverloadFunctionSymbol(this.Arguments.Length);
            this.Diagnostic
                .WithTemplate(TypeCheckingErrors.NoMatchingOverload)
                .WithFormatArgs(functionName);
            this.Promise.Fail(errorSymbol, diagnostics);
            yield return SolveState.Solved;
        }

        // We have one or more, find the max dominator
        var dominatingCandidates = GetDominatingCandidates(candidates);
        if (dominatingCandidates.Length == 1)
        {
            // Resolved fine, choose the symbol, which might generic-instantiate it
            var chosen = this.ChooseSymbol(dominatingCandidates[0]);

            // Inference
            if (chosen.IsVariadic)
            {
                if (!BinderFacts.TryGetVariadicElementType(chosen.Parameters[^1].Type, out var elementType))
                {
                    // Should not happen
                    throw new InvalidOperationException();
                }
                var nonVariadicPairs = chosen.Parameters
                    .SkipLast(1)
                    .Zip(this.Arguments);
                var variadicPairs = this.Arguments
                    .Skip(chosen.Parameters.Length - 1)
                    .Select(a => (ParameterType: elementType, ArgumentType: a));
                // Non-variadic part
                foreach (var (param, arg) in nonVariadicPairs) this.UnifyParameterWithArgument(param.Type, arg);
                // Variadic part
                foreach (var (paramType, arg) in variadicPairs) this.UnifyParameterWithArgument(paramType, arg);
            }
            else
            {
                foreach (var (param, arg) in chosen.Parameters.Zip(this.Arguments))
                {
                    this.UnifyParameterWithArgument(param.Type, arg);
                }
            }
            // NOTE: Unification won't always be correct, especially not when subtyping arises
            // In all cases, return type is simple
            this.Unify(this.ReturnType, chosen.ReturnType);
            // Resolve promise
            this.Promise.Resolve(chosen);
            yield return SolveState.Solved;
        }
        else
        {
            // Best-effort shape approximation
            this.Unify(this.ReturnType, IntrinsicSymbols.ErrorType);
            var errorSymbol = new NoOverloadFunctionSymbol(this.Arguments.Length);
            this.Diagnostic
                .WithTemplate(TypeCheckingErrors.AmbiguousOverloadedCall)
                .WithFormatArgs(functionName, string.Join(", ", dominatingCandidates));
            this.Promise.Fail(errorSymbol, diagnostics);
            yield return SolveState.Solved;
        }
    }

    private bool MatchesParameterCount(FunctionSymbol function)
    {
        // Exact count match is always eligibe by only param count
        if (function.Parameters.Length == this.Arguments.Length) return true;
        // If not variadic, we do need an exact match
        if (!function.IsVariadic) return false;
        // Otherise, there must be one less, exactly as many, or more arguments
        //  - one less means nullary variadics
        //  - exact match is one variadic
        //  - more is more variadics
        if (this.Arguments.Length + 1 >= function.Parameters.Length) return true;
        // No match
        return false;
    }

    private FunctionSymbol ChooseSymbol(FunctionSymbol chosen)
    {
        // Nongeneric, just return
        if (!chosen.IsGenericDefinition) return chosen;

        // Implicit generic instantiation
        // Create the proper number of type variables as type arguments
        var typeArgs = Enumerable
            .Range(0, chosen.GenericParameters.Length)
            .Select(_ => this.Solver.AllocateTypeVariable())
            .Cast<TypeSymbol>()
            .ToImmutableArray();

        // Instantiate the chosen symbol
        var instantiated = chosen.GenericInstantiate(chosen.ContainingSymbol, typeArgs);
        return instantiated;
    }

    private void UnifyParameterWithArgument(TypeSymbol paramType, object argument)
    {
        var promise = this.Solver.Assignable(paramType, ExtractType(argument));
        var syntax = ExtractSyntax(argument);
        if (syntax is not null)
        {
            promise.ConfigureDiagnostic(diag => diag.WithLocation(syntax.Location));
        }
    }

    private static ImmutableArray<FunctionSymbol> GetDominatingCandidates(IReadOnlyList<Candidate> candidates)
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

    private bool RefineScores(List<Candidate> candidates, out bool wellDefined)
    {
        var changed = false;
        wellDefined = true;
        // Iterate through all candidates
        for (var i = 0; i < candidates.Count;)
        {
            var candidate = candidates[i];

            // Compute any undefined arguments
            changed = this.AdjustScore(candidate) || changed;
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

    private bool AdjustScore(Candidate candidate)
    {
        var changed = false;
        var (func, scoreVector) = candidate;

        for (var i = 0; i < scoreVector.Length; ++i)
        {
            var param = func.Parameters[i];
            // Handle that separately
            if (param.IsVariadic) continue;

            if (this.Arguments.Length == i)
            {
                // Special case, this call was extended because of variadics
                if (scoreVector[i] is null)
                {
                    scoreVector[i] = FullScore;
                    changed = true;
                }
                continue;
            }

            var arg = ExtractType(this.Arguments[i]);
            var score = scoreVector[i];

            // If the argument is not null, it means we have already scored it
            if (score is not null) continue;

            score = ScoreArgument(param, arg);
            changed = changed || score is not null;
            scoreVector[i] = score;

            // If the score hit 0, terminate early, this overload got eliminated
            if (score == 0) return changed;
        }
        // Handle variadic arguments
        if (func.IsVariadic && scoreVector[^1] is null)
        {
            var variadicParam = func.Parameters[^1];
            var variadicArgTypes = this.Arguments
                .Skip(func.Parameters.Length - 1)
                .Select(ExtractType);
            var score = ScoreVariadicArguments(variadicParam, variadicArgTypes);
            changed = changed || score is not null;
            scoreVector[^1] = score;
        }
        return changed;
    }

    private static TypeSymbol ExtractType(object node) => node switch
    {
        UntypedExpression e => e.TypeRequired,
        UntypedLvalue l => l.Type,
        TypeSymbol t => t,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private static SyntaxNode? ExtractSyntax(object node) => node switch
    {
        UntypedNode n => n.Syntax,
        _ => null,
    };

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
