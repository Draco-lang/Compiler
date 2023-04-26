using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint for calling a single overload from a set of candidate functions.
/// </summary>
internal sealed class OverloadConstraint : Constraint<FunctionSymbol>
{
    /// <summary>
    /// The candidate functions to search among.
    /// </summary>
    public IConstraintPromise<ImmutableArray<Symbol>> Candidates { get; }

    /// <summary>
    /// The arguments the function was called with.
    /// </summary>
    public ImmutableArray<TypeSymbol> Arguments { get; }

    public OverloadConstraint(
        ConstraintSolver solver,
        IConstraintPromise<ImmutableArray<Symbol>> candidates,
        ImmutableArray<TypeSymbol> arguments)
        : base(solver)
    {
        this.Candidates = candidates;
        this.Arguments = arguments;
    }

    public override string ToString() => this.Candidates.IsResolved
        ? $"Overload(candidates: [{string.Join(", ", this.Candidates.Result)}], args: [{string.Join(", ", this.Arguments)}])"
        : $"Overload(candidates: ?, args: [{string.Join(", ", this.Arguments)}])";

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
        while (!this.Candidates.IsResolved) yield return SolveState.Stale;

        // TODO: Cast...
        var candidates = this.Candidates.Result
            .Cast<FunctionSymbol>()
            .ToList();
        var functionName = candidates[0].Name;
        var scoreVectors = this.InitializeScoreVectors(candidates);

        while (true)
        {
            var changed = this.RefineScores(candidates, scoreVectors, out var wellDefined);
            if (wellDefined) break;
            yield return changed ? SolveState.AdvancedContinue : SolveState.Stale;
        }

        // We have all candidates well-defined, find the absolute dominator
        if (candidates.Count == 0)
        {
            // Best-effort shape approximation
            var errorSymbol = new NoOverloadFunctionSymbol(this.Arguments.Length);
            this.Diagnostic
                .WithTemplate(TypeCheckingErrors.NoMatchingOverload)
                .WithFormatArgs(functionName);
            this.Promise.Fail(errorSymbol, diagnostics);
            yield return SolveState.Solved;
        }

        // We have one or more, find the max dominator
        // NOTE: This might not be the actual dominator in case of mutual non-dominance
        var bestScore = this.FindDominatorScoreVector(candidates, scoreVectors);
        // We keep every candidate that dominates this score, or there is mutual non-dominance
        var dominatingCandidates = candidates
            .Zip(scoreVectors)
            .Where(pair => Dominates(pair.Second, bestScore) || !Dominates(bestScore, pair.Second))
            .Select(pair => pair.First)
            .ToImmutableArray();
        Debug.Assert(dominatingCandidates.Length > 0);

        if (dominatingCandidates.Length == 1)
        {
            // Resolved fine
            this.Promise.Resolve(candidates[0]);
            yield return SolveState.Solved;
        }
        else
        {
            // Best-effort shape approximation
            var errorSymbol = new NoOverloadFunctionSymbol(this.Arguments.Length);
            this.Diagnostic
                .WithTemplate(TypeCheckingErrors.AmbiguousOverloadedCall)
                .WithFormatArgs(functionName, string.Join(", ", candidates));
            this.Promise.Fail(errorSymbol, diagnostics);
            yield return SolveState.Solved;
        }
    }

    private int?[] FindDominatorScoreVector(List<FunctionSymbol> candidates, List<int?[]> scoreVectors)
    {
        var bestScore = scoreVectors[0];
        for (var i = 1; i < candidates.Count; ++i)
        {
            var score = scoreVectors[i];

            if (Dominates(score, bestScore))
            {
                // Better, or equivalent
                bestScore = score;
            }
        }
        return bestScore;
    }

    private bool RefineScores(List<FunctionSymbol> candidates, List<int?[]> scoreVectors, out bool wellDefined)
    {
        var changed = false;
        wellDefined = true;
        // Iterate through all candidates
        for (var i = 0; i < candidates.Count;)
        {
            var candidate = candidates[i];
            var scoreVector = scoreVectors[i];

            // Compute any undefined arguments
            changed = this.AdjustScore(candidate, scoreVector) || changed;
            // We consider having a 0-element well-defined, since we are throwing it away
            var hasZero = HasZero(scoreVector);
            wellDefined = wellDefined && (IsWellDefined(scoreVector) || hasZero);

            // If any of the score vector components reached 0, we exclude the candidate
            if (hasZero)
            {
                candidates.RemoveAt(i);
                scoreVectors.RemoveAt(i);
            }
            else
            {
                // Otherwise it stays
                ++i;
            }
        }
        return changed;
    }

    private bool AdjustScore(FunctionSymbol candidate, int?[] scoreVector)
    {
        Debug.Assert(candidate.Parameters.Length == this.Arguments.Length);
        Debug.Assert(candidate.Parameters.Length == scoreVector.Length);

        var changed = false;
        for (var i = 0; i < scoreVector.Length; ++i)
        {
            var param = candidate.Parameters[i];
            var arg = this.Arguments[i];
            var score = scoreVector[i];

            // If the argument is not null, it means we have already scored it
            if (score is not null) continue;

            score = this.AdjustScore(param, arg);
            changed = changed || score is not null;
            scoreVector[i] = score;

            // If the score hit 0, terminate early, this overload got eliminated
            if (score == 0) return changed;
        }
        return changed;
    }

    private int? AdjustScore(ParameterSymbol param, TypeSymbol argType)
    {
        var paramType = this.Unwrap(param.Type);
        argType = this.Unwrap(argType);

        // If the argument is still a type parameter, we can't score it
        if (argType.IsTypeVariable) return null;

        // Exact equality is max score
        if (ReferenceEquals(paramType, argType)) return 16;

        // Otherwise, no match
        return 0;
    }

    private List<int?[]> InitializeScoreVectors(List<FunctionSymbol> candidates)
    {
        var scoreVectors = new List<int?[]>();

        for (var i = 0; i < candidates.Count;)
        {
            var candidate = candidates[i];

            // Broad filtering, skip candidates that have the wrong no. parameters
            if (candidate.Parameters.Length != this.Arguments.Length)
            {
                candidates.RemoveAt(i);
            }
            else
            {
                // Initialize score vector
                var scoreVector = new int?[this.Arguments.Length];
                scoreVectors.Add(scoreVector);
                ++i;
            }
        }

        return scoreVectors;
    }

    private static bool HasZero(int?[] vector) => vector.Any(x => x == 0);

    private static bool IsWellDefined(int?[] vector) => vector.All(x => x is not null);

    private static bool Dominates(int?[] a, int?[] b)
    {
        Debug.Assert(a.Length == b.Length);

        for (var i = 0; i < a.Length; ++i)
        {
            if (a[i] is null || b[i] is null) return false;
            if (a[i] < b[i]) return false;
        }

        return true;
    }
}
