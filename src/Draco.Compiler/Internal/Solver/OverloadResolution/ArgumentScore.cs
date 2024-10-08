using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.OverloadResolution;

/// <summary>
/// Utility for argument scoring.
/// </summary>
internal static class ArgumentScore
{
    /// <summary>
    /// An undefined score.
    /// </summary>
    public const int Undefined = -1;

    /// <summary>
    /// Maximum score for a full match.
    /// </summary>
    public const int FullScore = 32;

    private const int HalfScore = FullScore / 2;
    private const int ZeroScore = 0;

    /// <summary>
    /// Scores a sequence of variadic function call argument.
    /// </summary>
    /// <param name="param">The variadic function parameter.</param>
    /// <param name="args">The passed in arguments.</param>
    /// <returns>The score of the match.</returns>
    public static int ScoreVariadicArguments(ParameterSymbol param, IEnumerable<Argument> args) =>
        ScoreVariadicArguments(param, args.Select(arg => arg.Type));

    /// <summary>
    /// Scores a sequence of variadic function call argument.
    /// </summary>
    /// <param name="param">The variadic function parameter.</param>
    /// <param name="argTypes">The passed in argument types.</param>
    /// <returns>The score of the match.</returns>
    public static int ScoreVariadicArguments(ParameterSymbol param, IEnumerable<TypeSymbol> argTypes)
    {
        if (!param.IsVariadic) throw new ArgumentException("the provided parameter is not variadic", nameof(param));
        if (!BinderFacts.TryGetVariadicElementType(param.Type, out var elementType)) return 0;

        return argTypes
            // Score each argument
            .Select(argType => ScoreArgument(elementType, argType))
            // In case the sequence is empty, we assume a full score match
            .Append(FullScore)
            // Every variadic argument is half as important as a normal argument
            .Select(s => s / 2)
            // Take the lowest score
            .Min();
    }

    /// <summary>
    /// Scores a function call argument.
    /// </summary>
    /// <param name="param">The function parameter.</param>
    /// <param name="arg">The passed in argument.</param>
    /// <returns>The score of the match.</returns>
    public static int ScoreArgument(ParameterSymbol param, Argument arg) =>
        ScoreArgument(param, arg.Type);

    /// <summary>
    /// Scores a function call argument.
    /// </summary>
    /// <param name="param">The function parameter.</param>
    /// <param name="argType">The passed in argument type.</param>
    /// <returns>The score of the match.</returns>
    public static int ScoreArgument(ParameterSymbol param, TypeSymbol argType)
    {
        if (param.IsVariadic) throw new ArgumentException("the provided parameter variadic", nameof(param));
        return ScoreArgument(param.Type, argType);
    }

    private static int ScoreArgument(TypeSymbol paramType, TypeSymbol argType)
    {
        paramType = paramType.Substitution;
        argType = argType.Substitution;

        // If either are still not ground types, we can't decide
        if (!paramType.IsGroundType || !argType.IsGroundType) return Undefined;

        // Exact equality is max score
        if (SymbolEqualityComparer.Default.Equals(paramType, argType)) return FullScore;

        // Base type match is half score
        if (SymbolEqualityComparer.Default.IsBaseOf(paramType, argType)) return HalfScore;

        // For generics we take the lowest scoring argument and half it, if the generic type is a base
        if (paramType.IsGenericInstance && argType.IsGenericInstance)
        {
            var paramGenericDefinition = paramType.GenericDefinition!;
            var argGenericDefinition = argType.GenericDefinition!;

            var genericDefinitionIsExact = SymbolEqualityComparer.Default.Equals(paramGenericDefinition, argGenericDefinition);
            var genericDefinitionIsBase = SymbolEqualityComparer.Default.IsBaseOf(paramGenericDefinition, argGenericDefinition);

            if (!genericDefinitionIsExact && !genericDefinitionIsBase) return ZeroScore;

            Debug.Assert(paramType.GenericArguments.Length == argType.GenericArguments.Length);
            var minGenericScore = paramType.GenericArguments
                .Zip(argType.GenericArguments)
                .Select(pair => ScoreArgument(pair.First, pair.Second))
                .Min();

            return genericDefinitionIsExact ? minGenericScore : minGenericScore / 2;
        }

        // Type parameter match is half score
        if (paramType is TypeParameterSymbol) return HalfScore;

        // Otherwise, no match
        return ZeroScore;
    }
}
