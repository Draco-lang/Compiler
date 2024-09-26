using System;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Symbols.Synthetized.Array;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    /// <summary>
    /// Unifies two types, asserting their success.
    /// </summary>
    /// <param name="first">The first type to unify.</param>
    /// <param name="second">The second type to unify.</param>
    public static void UnifyAsserted(TypeSymbol first, TypeSymbol second)
    {
        if (Unify(first, second)) return;
        throw new InvalidOperationException($"could not unify {first} and {second}");
    }

    /// <summary>
    /// Attempts to unify two types.
    /// </summary>
    /// <param name="first">The first type to unify.</param>
    /// <param name="second">The second type to unify.</param>
    /// <returns>True, if unification was successful, false otherwise.</returns>
    public static bool Unify(TypeSymbol first, TypeSymbol second)
    {
        first = first.Substitution;
        second = second.Substitution;

        // NOTE: Referential equality is OK here, we don't need to use SymbolEqualityComparer, this is unification
        if (ReferenceEquals(first, second)) return true;

        switch (first, second)
        {
        // Type variable substitution takes priority
        // so it can unify with never type and error type to stop type errors from cascading
        case (TypeVariable v1, TypeVariable v2):
        {
            // Check for circularity
            // NOTE: Referential equality is OK here, we are checking for CIRCULARITY
            // which is  referential check
            if (ReferenceEquals(v1, v2)) return true;
            v1.Substitute(v2);
            return true;
        }
        case (TypeVariable v, TypeSymbol other):
        {
            v.Substitute(other);
            return true;
        }
        case (TypeSymbol other, TypeVariable v):
        {
            v.Substitute(other);
            return true;
        }

        // Never type is never reached, unifies with everything
        case (NeverTypeSymbol, _):
        case (_, NeverTypeSymbol):
        // Error type unifies with everything to avoid cascading type errors
        case (ErrorTypeSymbol, _):
        case (_, ErrorTypeSymbol):
            return true;

        case (ArrayTypeSymbol a1, ArrayTypeSymbol a2) when a1.IsGenericDefinition && a2.IsGenericDefinition:
            return a1.Rank == a2.Rank;

        // NOTE: Primitives are filtered out already, along with metadata types

        case (FunctionTypeSymbol f1, FunctionTypeSymbol f2):
        {
            if (f1.Parameters.Length != f2.Parameters.Length) return false;
            for (var i = 0; i < f1.Parameters.Length; ++i)
            {
                if (!Unify(f1.Parameters[i].Type, f2.Parameters[i].Type)) return false;
            }
            return Unify(f1.ReturnType, f2.ReturnType);
        }

        case (_, _) when first.IsGenericInstance && second.IsGenericInstance:
        {
            // NOTE: Generic instances might not obey referential equality
            Debug.Assert(first.GenericDefinition is not null);
            Debug.Assert(second.GenericDefinition is not null);
            if (first.GenericArguments.Length != second.GenericArguments.Length) return false;
            if (!Unify(first.GenericDefinition, second.GenericDefinition)) return false;
            for (var i = 0; i < first.GenericArguments.Length; ++i)
            {
                if (!Unify(first.GenericArguments[i], second.GenericArguments[i])) return false;
            }
            return true;
        }

        default:
            return false;
        }
    }

    /// <summary>
    /// Unified a type with the error type.
    /// Does not assert the unification success, this is an error-cascading measure.
    /// </summary>
    /// <param name="type">The type to unify with the error type.</param>
    public static void UnifyError(TypeSymbol type) => Unify(type, WellKnownTypes.ErrorType);

    /// <summary>
    /// Assigns a type to another, asserting their success.
    /// </summary>
    /// <param name="targetType">The target type to assign to.</param>
    /// <param name="assignedType">The assigned type.</param>
    public static void AssignAsserted(TypeSymbol targetType, TypeSymbol assignedType)
    {
        if (Assign(targetType, assignedType)) return;
        throw new InvalidOperationException($"could not assign {assignedType} to {targetType}");
    }

    /// <summary>
    /// Assigns a type to anoter.
    /// </summary>
    /// <param name="targetType">The target type to assign to.</param>
    /// <param name="assignedType">The assigned type.</param>
    /// <returns>True, if the assignment was successful, false otherwise.</returns>
    private static bool Assign(TypeSymbol targetType, TypeSymbol assignedType)
    {
        targetType = targetType.Substitution;
        assignedType = assignedType.Substitution;

        if (targetType.IsGenericInstance && assignedType.IsGenericInstance)
        {
            // We need to look for the base type
            var targetGenericDefinition = targetType.GenericDefinition!;

            var assignedToUnify = assignedType.BaseTypes
                .FirstOrDefault(t => SymbolEqualityComparer.Default.Equals(t.GenericDefinition, targetGenericDefinition));
            if (assignedToUnify is null)
            {
                // TODO
                throw new NotImplementedException();
            }

            // Unify
            return Unify(targetType, assignedToUnify);
        }
        else
        {
            // TODO: Might not be correct
            return Unify(targetType, assignedType);
        }
    }
}
