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
        var candidates = this.Candidates.ToList();
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
        // NOTE: This might not be the actual dominator in case of mutual non-dominance
        var bestScore = CallScore.FindBest(scoreVectors);
        // We keep every candidate that dominates this score, or there is mutual non-dominance
        var dominatingCandidates = candidates
            .Zip(scoreVectors)
            .Where(pair => bestScore is null
                        || CallScore.Compare(bestScore.Value, pair.Second) is CallScoreComparison.Equal or CallScoreComparison.NoDominance)
            .Select(pair => pair.First)
            .ToImmutableArray();
        Debug.Assert(dominatingCandidates.Length > 0);

        if (dominatingCandidates.Length == 1)
        {
            // Resolved fine
            this.Unify(this.ReturnType, dominatingCandidates[0].ReturnType);
            this.Promise.Resolve(dominatingCandidates[0]);
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

    private bool RefineScores(List<FunctionSymbol> candidates, List<CallScore> scoreVectors, out bool wellDefined)
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
            var hasZero = scoreVector.HasZero;
            wellDefined = wellDefined && (scoreVector.IsWellDefined || hasZero);

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

    private bool AdjustScore(FunctionSymbol candidate, CallScore scoreVector)
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

            score = this.Solver.ScoreArgument(param, arg);
            changed = changed || score is not null;
            scoreVector[i] = score;

            // If the score hit 0, terminate early, this overload got eliminated
            if (score == 0) return changed;
        }
        return changed;
    }

    private List<CallScore> InitializeScoreVectors(List<FunctionSymbol> candidates)
    {
        var scoreVectors = new List<CallScore>();

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
                var scoreVector = new CallScore(this.Arguments.Length);
                scoreVectors.Add(scoreVector);
                ++i;
            }
        }

        return scoreVectors;
    }
}
