using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents an uninferred type that can be substituted.
/// </summary>
internal sealed class TypeVariable : TypeSymbol
{
    public override bool IsTypeVariable => true;
    public override Symbol? ContainingSymbol => throw new NotImplementedException();

    private readonly int index;

    public TypeVariable(int index)
    {
        this.index = index;
    }

    public override string ToString() => $"{StringUtils.IndexToExcelColumnName(this.index)}'";
}
