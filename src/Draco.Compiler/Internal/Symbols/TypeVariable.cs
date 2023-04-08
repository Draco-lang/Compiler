using System;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents an uninferred type that can be substituted.
/// </summary>
internal sealed class TypeVariable : TypeSymbol
{
    public override bool IsTypeVariable => true;
    public override bool IsError => throw new NotSupportedException();
    public override Symbol? ContainingSymbol => throw new NotSupportedException();

    private readonly int index;

    public TypeVariable(int index)
    {
        this.index = index;
    }

    public override string ToString() => $"{StringUtils.IndexToExcelColumnName(this.index)}'";
}
