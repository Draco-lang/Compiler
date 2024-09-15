using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Symbols.Synthetized.Array;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Base of equality comparers for symbols.
/// </summary>
internal sealed class SymbolEqualityComparer : IEqualityComparer<Symbol>, IEqualityComparer<TypeSymbol>
{
    [Flags]
    private enum ComparerFlags
    {
        None = 0,
        EquateGenericParameters = 1 << 0,
        AllowTypeVariables = 1 << 1,
    }

    /// <summary>
    /// A default symbol equality comparer.
    /// </summary>
    public static SymbolEqualityComparer Default { get; } = new(ComparerFlags.None);

    /// <summary>
    /// A symbol equality comparer that can be used for signature matching.
    /// </summary>
    public static SymbolEqualityComparer SignatureMatch { get; } = new(ComparerFlags.EquateGenericParameters);

    /// <summary>
    /// A symbol equality comparer that equates type variables that are referentially equal.
    /// </summary>
    public static SymbolEqualityComparer AllowTypeVariables { get; } = new(ComparerFlags.AllowTypeVariables);

    private readonly ComparerFlags flags;

    private SymbolEqualityComparer(ComparerFlags flags)
    {
        this.flags = flags;
    }

    /// <summary>
    /// Checks, if a type is a base of another.
    /// </summary>
    /// <param name="base">The type that is assumned to be the base of <paramref name="derived"/>.</param>
    /// <param name="derived">The type that's assumed to be a subtype of <paramref name="base"/>.</param>
    /// <returns>True, if <paramref name="base"/> is a base type of <paramref name="derived"/>.</returns>
    public bool IsBaseOf(TypeSymbol @base, TypeSymbol derived)
    {
        @base = this.Unwrap(@base);
        derived = this.Unwrap(derived);

        if (@base is NeverTypeSymbol or ErrorTypeSymbol) return true;
        if (derived is NeverTypeSymbol or ErrorTypeSymbol) return true;

        return derived.BaseTypes.Contains(@base, this);
    }

    public bool Equals(Symbol? x, Symbol? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x is TypeSymbol xType && y is TypeSymbol yType) return this.Equals(xType, yType);
        return false;
    }

    public bool Equals(TypeSymbol? x, TypeSymbol? y)
    {
        x = this.Unwrap(x);
        y = this.Unwrap(y);

        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        if (x.IsGenericInstance && y.IsGenericInstance)
        {
            // Generic instances might not adhere to referential equality
            // Instead we check if the generic definitions and the arguments are equal

            // TODO: Should we check for the entire context equality?
            // Could context affect the type here in any significant way, for ex. in the
            // case of nested generic types?
            // The problem with that would be that not all context variables are significant tho,
            // whe might need to end up projecting down generic args like C# does?

            if (x.GenericArguments.Length != y.GenericArguments.Length) return false;
            if (!this.Equals(x.GenericDefinition, y.GenericDefinition)) return false;
            return x.GenericArguments.SequenceEqual(y.GenericArguments, this);
        }

        // TODO: Does this check belong here?
        if (x.IsGenericDefinition && IsUnboundedGenericInstance(y))
        {
            // Check, if x is a generic bound instance, meaning its arguments are also generic types
            // TODO: This might be a nice place to check constraints in the future too?
            return this.Equals(x, y.GenericDefinition);
        }

        // TODO: Does this check belong here?
        if (y.IsGenericDefinition && IsUnboundedGenericInstance(x))
        {
            // Check, if x is a generic bound instance, meaning its arguments are also generic types
            // TODO: This might be a nice place to check constraints in the future too?
            return this.Equals(x.GenericDefinition, y);
        }

        return (x, y) switch
        {
            (ArrayTypeSymbol a1, ArrayTypeSymbol a2)
                when a1.IsGenericDefinition && a2.IsGenericDefinition => a1.Rank == a2.Rank,
            (FunctionTypeSymbol f1, FunctionTypeSymbol f2) =>
                   f1.Parameters.SequenceEqual(f2.Parameters, this)
                && this.Equals(f1.ReturnType, f2.ReturnType),
            (TypeParameterSymbol, TypeParameterSymbol)
                when this.flags.HasFlag(ComparerFlags.EquateGenericParameters) => true,
            _ => false,
        };
    }

    public int GetHashCode([DisallowNull] Symbol obj) => obj switch
    {
        TypeSymbol t => this.GetHashCode(t),
        _ when obj.IsGenericInstance => GetGenericInstanceHashCode(obj),
        _ => RuntimeHelpers.GetHashCode(obj),
    };

    private static int GetGenericInstanceHashCode(Symbol obj)
    {
        // Combine the hash code of the generic definition with the hash codes of the arguments
        var hash = default(HashCode);
        hash.Add(obj.GenericDefinition);
        foreach (var arg in obj.GenericArguments) hash.Add(arg);
        return hash.ToHashCode();
    }

    public int GetHashCode([DisallowNull] TypeSymbol obj)
    {
        obj = this.Unwrap(obj);

        if (obj.IsGenericInstance)
        {
            // Combine the hash code of the generic definition with the hash codes of the arguments
            var hash = default(HashCode);
            hash.Add(obj.GenericDefinition);
            foreach (var arg in obj.GenericArguments) hash.Add(arg);
            return hash.ToHashCode();
        }

        return RuntimeHelpers.GetHashCode(obj);
    }

    [return: NotNullIfNotNull(nameof(type))]
    private TypeSymbol? Unwrap(TypeSymbol? type)
    {
        if (type is null) return null;

        var unwrappedType = type.Substitution;
        if (!this.flags.HasFlag(ComparerFlags.AllowTypeVariables) && unwrappedType.IsTypeVariable)
        {
            throw new InvalidOperationException("could not unwrap type variable");
        }
        return unwrappedType;
    }

    private static bool IsUnboundedGenericInstance(TypeSymbol t) =>
        t.IsGenericInstance && t.GenericArguments.All(a => a.Substitution is TypeParameterSymbol);
}
