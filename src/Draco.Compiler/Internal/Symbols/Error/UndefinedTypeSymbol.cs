using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an undefined, in-source type reference.
/// </summary>
internal sealed class UndefinedTypeSymbol : TypeSymbol
{
    public override bool IsError => true;
    public override Symbol? ContainingSymbol => null;

    public override string Name { get; }

    public UndefinedTypeSymbol(string name)
    {
        this.Name = name;
    }

    public override string ToString() => this.Name;
}
