using System;
using System.Collections.Generic;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents an uninferred type that can be substituted.
/// </summary>
internal sealed class TypeVariable : TypeSymbol
{
    public override bool IsTypeVariable => true;
    public override bool IsValueType => throw new NotSupportedException();
    public override bool IsError => throw new NotSupportedException();
    public override Symbol? ContainingSymbol => throw new NotSupportedException();
    public override IEnumerable<Symbol> Members => throw new NotSupportedException();
    public override string Documentation => throw new NotSupportedException();

    private readonly int index;

    public TypeVariable(int index)
    {
        this.index = index;
    }

    public override string ToString() => $"{StringUtils.IndexToExcelColumnName(this.index)}'";

    public override void Accept(SymbolVisitor visitor) => throw new NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new NotSupportedException();
}
