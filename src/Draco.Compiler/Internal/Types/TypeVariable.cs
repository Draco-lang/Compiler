using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents an uninferred type that can be substituted.
/// </summary>
internal sealed class TypeVariable : Type
{
    public override bool IsTypeVariable => true;

    private readonly int index;

    public TypeVariable(int index)
    {
        this.index = index;
    }

    public override string ToString() => $"{StringUtils.IndexToExcelColumnName(this.index)}'";
}
