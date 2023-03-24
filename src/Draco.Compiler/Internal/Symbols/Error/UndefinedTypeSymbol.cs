using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an undefined, in-source type reference.
/// </summary>
internal sealed class UndefinedTypeSymbol : TypeSymbol
{
    public override bool IsError => true;
    public override Type Type => IntrinsicTypes.Error;
    public override Symbol? ContainingSymbol => null;

    public override string Name { get; }

    public UndefinedTypeSymbol(string name)
    {
        this.Name = name;
    }
}
