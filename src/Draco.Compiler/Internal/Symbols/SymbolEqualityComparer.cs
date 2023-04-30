using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Base of equality comparers for symbols.
/// </summary>
internal abstract class SymbolEqualityComparer : IEqualityComparer<Symbol>, IEqualityComparer<TypeSymbol>
{
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

        if (x is TypeVariable xTypeVar) x = this.Unwrap(xTypeVar);
        if (y is TypeVariable yTypeVar) y = this.Unwrap(yTypeVar);

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
        if (obj is TypeVariable v) obj = this.Unwrap(v);

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
    protected abstract TypeSymbol Unwrap(TypeVariable type);
}
