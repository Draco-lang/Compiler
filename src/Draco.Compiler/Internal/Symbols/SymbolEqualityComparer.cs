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

        if (x is TypeVariable xTypeVar) x = this.UnwrapTypeVariable(xTypeVar);
        if (y is TypeVariable yTypeVar) y = this.UnwrapTypeVariable(yTypeVar);

        // TODO
        throw new NotImplementedException();
    }

    public int GetHashCode([DisallowNull] Symbol obj) => throw new NotImplementedException();
    public int GetHashCode([DisallowNull] TypeSymbol obj) => throw new NotImplementedException();

    protected abstract TypeSymbol UnwrapTypeVariable(TypeVariable type);
}
