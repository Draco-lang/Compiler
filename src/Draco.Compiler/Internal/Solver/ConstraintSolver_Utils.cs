using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private readonly record struct OverloadCandidate(FunctionSymbol Symbol, CallScore Score);

    private FunctionTypeSymbol MakeMismatchedFunctionType(ImmutableArray<Argument> args, TypeSymbol returnType) => new(
        args
            // TODO: We are passing null here...
            .Select(a => new SynthetizedParameterSymbol(null!, a.Type))
            .Cast<ParameterSymbol>()
            .ToImmutableArray(),
        returnType);

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
        ConstraintLocator.Syntax(argument.Syntax));

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
            if (score != ArgumentScore.Undefined) continue;

            score = ArgumentScore.ScoreArgument(param, arg.Type);
            changed = changed || score != ArgumentScore.Undefined;
            scoreVector[i] = score;

            // If the score hit 0, terminate early, this overload got eliminated
            if (score == 0) return changed;
        }
        return changed;
    }
}
