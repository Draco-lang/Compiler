using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

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

    public OverloadConstraint(
        ConstraintSolver solver,
        IEnumerable<FunctionSymbol> candidates,
        ImmutableArray<TypeSymbol> arguments)
        : base(solver)
    {
        this.Candidates = candidates;
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
        for (var i = 0; i < this.Candidates.Count;)
        {
            var candidate = this.Candidates[i];
            var scoreVector = this.scoreVectors[i];

            // TODO
            throw new NotImplementedException();
        }

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
