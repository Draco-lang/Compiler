using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a callability constraint for indirect calls.
/// </summary>
internal sealed class CallConstraint : Constraint<Unit>
{
    /// <summary>
    /// The called expression type.
    /// </summary>
    public TypeSymbol CalledType { get; }

    /// <summary>
    /// The arguments the function was called with.
    /// </summary>
    public ImmutableArray<TypeSymbol> Arguments { get; }

    /// <summary>
    /// The return type of the call.
    /// </summary>
    public TypeSymbol ReturnType { get; }

    public CallConstraint(
        ConstraintSolver solver,
        TypeSymbol calledType,
        ImmutableArray<TypeSymbol> arguments,
        TypeSymbol returnType)
        : base(solver)
    {
        this.CalledType = calledType;
        this.Arguments = arguments;
        this.ReturnType = returnType;
    }

    public override string ToString() =>
        $"Call(function: {this.CalledType}, args: [{string.Join(", ", this.Arguments)}]) => {this.ReturnType}";

    public override void FailSilently()
    {
        this.Unify(this.ReturnType, IntrinsicSymbols.ErrorType);
        this.Promise.Fail(default, null);
    }

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
    start:
        var called = this.Unwrap(this.CalledType);
        // We can't advance on type variables
        if (called.IsTypeVariable)
        {
            yield return SolveState.Stale;
            goto start;
        }

        if (called.IsError)
        {
            // Don't propagate errors
            this.FailSilently();
            yield return SolveState.Solved;
        }

        // We can now check if it's a function
        if (called is not FunctionTypeSymbol functionType)
        {
            // Error
            // TODO
            throw new NotImplementedException();
            yield return SolveState.Solved;
        }

        // It's a function
        // We can merge the return type
        this.Unify(this.ReturnType, functionType.ReturnType);
        yield return SolveState.AdvancedContinue;

        // Check if it has the same number of args
        if (functionType.Parameters.Length != this.Arguments.Length)
        {
            // TODO
            throw new NotImplementedException();
            yield return SolveState.Solved;
        }

        // Start scoring args
        var score = new CallScore(functionType.Parameters.Length);
        while (true)
        {
            var changed = this.AdjustScore(functionType, score);
            if (score.HasZero)
            {
                // TODO
                throw new NotImplementedException();
                yield return SolveState.Solved;
            }
            if (score.IsWellDefined) break;
            yield return changed ? SolveState.AdvancedContinue : SolveState.Stale;
        }

        yield return SolveState.Solved;
    }

    private bool AdjustScore(FunctionTypeSymbol candidate, CallScore scoreVector)
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
}
