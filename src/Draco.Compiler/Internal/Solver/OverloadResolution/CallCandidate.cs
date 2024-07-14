using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.OverloadResolution;

/// <summary>
/// Represents a single candidate for overload resolution.
/// </summary>
internal readonly struct CallCandidate(FunctionSymbol symbol)
{
    /// <summary>
    /// THe function being called.
    /// </summary>
    public FunctionSymbol Symbol { get; } = symbol;

    /// <summary>
    /// The score of the candidate.
    /// </summary>
    public CallScore Score { get; } = new(symbol.Parameters.Length);

    /// <summary>
    /// True, if the candidate is eliminated.
    /// </summary>
    public bool IsEliminated => this.Score.HasZero;

    /// <summary>
    /// True, if the candidate is well defined.
    /// </summary>
    public bool IsWellDefined => this.Score.IsWellDefined;

    /// <summary>
    /// Refines the candidate by scoring the arguments.
    /// </summary>
    /// <param name="arguments">The arguments to use for the refinement.</param>
    /// <returns>True, if the score got changed in any way.</returns>
    public bool Refine(IReadOnlyList<Argument> arguments)
    {
        var changed = false;
        var scoreVector = this.Score;

        for (var i = 0; i < scoreVector.Length; ++i)
        {
            var param = this.Symbol.Parameters[i];
            // Handle that separately
            if (param.IsVariadic) continue;

            if (arguments.Count == i)
            {
                // Special case, this call was extended because of variadics
                if (scoreVector[i] == ArgumentScore.Undefined)
                {
                    scoreVector[i] = ArgumentScore.FullScore;
                    changed = true;
                }
                continue;
            }

            var argType = arguments[i].Type;
            var score = scoreVector[i];

            // If the argument is not null, it means we have already scored it
            if (score != ArgumentScore.Undefined) continue;

            score = ArgumentScore.ScoreArgument(param, argType);
            changed = changed || score != ArgumentScore.Undefined;
            scoreVector[i] = score;

            // If the score hit 0, terminate early, this candidate got eliminated
            if (score == 0) return changed;
        }
        // Handle variadic arguments
        if (this.Symbol.IsVariadic && scoreVector[^1] == ArgumentScore.Undefined)
        {
            var variadicParam = this.Symbol.Parameters[^1];
            var variadicArgs = arguments.Skip(this.Symbol.Parameters.Length - 1);
            var score = ArgumentScore.ScoreVariadicArguments(variadicParam, variadicArgs);
            changed = changed || score != ArgumentScore.Undefined;
            scoreVector[^1] = score;
        }
        return changed;
    }
}
