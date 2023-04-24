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
    public IList<FunctionSymbol> Candidates { get; }

    /// <summary>
    /// The arguments the function was called with.
    /// </summary>
    public ImmutableArray<TypeSymbol> Arguments { get; }

    /// <summary>
    /// The name of the called function, kept for diagnostics.
    /// </summary>
    public string FunctionName { get; }

    public override IEnumerable<TypeVariable> TypeVariables =>
        this.Arguments.OfType<TypeVariable>();

    private List<int?[]>? scoreVectors;
    private bool allWellDefined;

    public OverloadConstraint(
        ConstraintSolver solver,
        IEnumerable<FunctionSymbol> candidates,
        ImmutableArray<TypeSymbol> arguments)
        : base(solver)
    {
        this.Candidates = candidates.ToList();
        this.Arguments = arguments;
        this.FunctionName = this.Candidates.First().Name;
    }

    public override string ToString() =>
        $"Overload(candidates: [{string.Join(", ", this.Candidates)}], args: [{string.Join(", ", this.Arguments)}])";

    public override SolveState Solve(DiagnosticBag diagnostics)
    {
        if (this.scoreVectors is null)
        {
            // Score vectors have not been calculated yet
            this.scoreVectors = this.InitializeScoreVectors();
            return SolveState.Advanced;
        }

        // The score vectors are already initialized
        Debug.Assert(this.Candidates.Count == this.scoreVectors.Count);

        if (!this.allWellDefined)
        {
            // We don't have all candidates well-defined, reiterate
            var changed = this.RefineScores();
            return changed ? SolveState.Advanced : SolveState.Stale;
        }

        // We have all candidates well-defined, find the absolute dominator
        if (this.Candidates.Count == 0)
        {
            // Best-effort shape approximation
            var errorSymbol = new NoOverloadFunctionSymbol(this.Arguments.Length);
            this.Diagnostic
                .WithTemplate(TypeCheckingErrors.NoMatchingOverload)
                .WithFormatArgs(this.FunctionName);
            this.Promise.Fail(errorSymbol, diagnostics);
            return SolveState.Solved;
        }

        // We have one or more, find the max dominator
        // NOTE: This might not be the actual dominator in case of mutual non-dominance
        var bestScore = this.FindDominatorScoreVector();
        // We keep every candidate that dominates this score, or there is mutual non-dominance
        var candidates = this.Candidates
            .Zip(this.scoreVectors)
            .Where(pair => Dominates(pair.Second, bestScore) || !Dominates(bestScore, pair.Second))
            .Select(pair => pair.First)
            .ToImmutableArray();
        Debug.Assert(candidates.Length > 0);

        if (candidates.Length == 1)
        {
            // Resolved fine
            this.Promise.Resolve(candidates[0]);
            return SolveState.Solved;
        }
        else
        {
            // Best-effort shape approximation
            var errorSymbol = new NoOverloadFunctionSymbol(this.Arguments.Length);
            this.Diagnostic
                .WithTemplate(TypeCheckingErrors.AmbiguousOverloadedCall)
                .WithFormatArgs(this.FunctionName, string.Join(", ", candidates));
            this.Promise.Fail(errorSymbol, diagnostics);
            return SolveState.Solved;
        }
    }

    private int?[] FindDominatorScoreVector()
    {
        Debug.Assert(this.scoreVectors is not null);

        var bestScore = this.scoreVectors[0];
        for (var i = 1; i < this.Candidates.Count; ++i)
        {
            var score = this.scoreVectors[i];

            if (Dominates(score, bestScore))
            {
                // Better, or equivalent
                bestScore = score;
            }
        }
        return bestScore;
    }

    private bool RefineScores()
    {
        Debug.Assert(this.scoreVectors is not null);
        Debug.Assert(this.Candidates.Count == this.scoreVectors.Count);

        var wellDefined = true;
        var changed = false;
        // Iterate through all candidates
        for (var i = 0; i < this.Candidates.Count;)
        {
            var candidate = this.Candidates[i];
            var scoreVector = this.scoreVectors[i];

            // Compute any undefined arguments
            changed = this.AdjustScore(candidate, scoreVector) || changed;
            // We consider having a 0-element well-defined, since we are throwing it away
            var hasZero = HasZero(scoreVector);
            wellDefined = wellDefined && (IsWellDefined(scoreVector) || hasZero);

            // If any of the score vector components reached 0, we exclude the candidate
            if (hasZero)
            {
                this.Candidates.RemoveAt(i);
                this.scoreVectors.RemoveAt(i);
            }
            else
            {
                // Otherwise it stays
                ++i;
            }
        }
        // Change
        changed = changed || (this.allWellDefined != wellDefined);
        this.allWellDefined = wellDefined;
        return changed;
    }

    private bool AdjustScore(FunctionSymbol candidate, int?[] scoreVector)
    {
        Debug.Assert(candidate.Parameters.Length == scoreVector.Length);

        // TODO
        throw new NotImplementedException();
    }

    private List<int?[]> InitializeScoreVectors()
    {
        var scoreVectors = new List<int?[]>();

        for (var i = 0; i < this.Candidates.Count;)
        {
            var candidate = this.Candidates[i];

            // Broad filtering, skip candidates that have the wrong no. parameters
            if (candidate.Parameters.Length != this.Arguments.Length)
            {
                this.Candidates.RemoveAt(i);
            }
            else
            {
                // Initialize score vector
                var scoreVector = new int?[this.Arguments.Length];
                Array.Fill(scoreVector, null);
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
