using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Solver;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Base of equality comparers for symbols.
/// </summary>
internal sealed class SymbolEqualityComparer : IEqualityComparer<Symbol>, IEqualityComparer<TypeSymbol>
{
    /// <summary>
    /// A default symbol equality comparer.
    /// </summary>
    public static SymbolEqualityComparer Default { get; } = new();

    private SymbolEqualityComparer()
    {
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
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        if (x is TypeVariable xTypeVar) x = Unwrap(xTypeVar);
        if (y is TypeVariable yTypeVar) y = Unwrap(yTypeVar);

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

        return (x, y) switch
        {
            (FunctionTypeSymbol f1, FunctionTypeSymbol f2) =>
                   f1.Parameters.SequenceEqual(f2.Parameters, this)
                && this.Equals(f1.ReturnType, f2.ReturnType),
            _ => false,
        };
    }

    public int GetHashCode([DisallowNull] Symbol obj) => obj switch
    {
        TypeSymbol t => this.GetHashCode(t),
        _ => throw new ArgumentOutOfRangeException(nameof(obj)),
    };

    public int GetHashCode([DisallowNull] TypeSymbol obj)
    {
        if (obj is TypeVariable v) obj = v;

        switch (obj)
        {
        default:
            throw new ArgumentOutOfRangeException(nameof(obj));
        }
    }

    /// <summary>
    /// Unwraps the given type-variable.
    /// </summary>
    /// <param name="type">The type-variable to unwrap.</param>
    /// <returns>The substitution of <paramref name="type"/>.</returns>
    private static TypeSymbol Unwrap(TypeVariable type)
    {
        var unwrappedType = type.Substitution;
        if (unwrappedType.IsTypeVariable) throw new InvalidOperationException("could not unwrap type variable");
        return unwrappedType;
    }
}
