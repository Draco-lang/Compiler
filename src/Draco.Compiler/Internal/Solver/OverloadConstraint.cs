using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;

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
    public ImmutableArray<TypeSymbol> Arguments { get; }

    /// <summary>
    /// The return type of the call.
    /// </summary>
    public TypeSymbol ReturnType { get; }

    public OverloadConstraint(
        ConstraintSolver solver,
        ImmutableArray<FunctionSymbol> candidates,
        ImmutableArray<TypeSymbol> arguments,
        TypeSymbol returnType)
        : base(solver)
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
        var candidates = this.Candidates
            .Where(this.MatchesParameterCount)
            .Select(f => new Candidate(f, new CallScore(f.Parameters.Length)))
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
            // NOTE: Unification won't always be correct, especially not when subtyping arises
            foreach (var (param, argType) in chosen.Parameters.Zip(this.Arguments)) this.Unify(param.Type, argType);
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
        for (var i = 0; i < candidate.Score.Length; ++i)
        {
            var (func, scoreVector) = candidate;

            var param = func.Parameters[i];
            var arg = this.Arguments[i];
            var score = scoreVector[i];

            // If the argument is not null, it means we have already scored it
            if (score is not null) continue;

            score = this.Solver.ScoreArgument(param, arg);
            changed = changed || score is not null;
            scoreVector[i] = score;

            // If the score hit 0, terminate early, this overload got eliminated
            if (score == 0) return changed;
        }
        return changed;
    }
}
