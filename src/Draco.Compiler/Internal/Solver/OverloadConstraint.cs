using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            this.scoreVectors = new();

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
                    this.scoreVectors.Add(scoreVector);
                    ++i;
                }
            }
            return SolveState.Advanced;
        }

        // The score vectors are already initialized
        // TODO
        throw new NotImplementedException();
    }
}
