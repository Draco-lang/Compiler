namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A built-in primitive.
/// </summary>
internal sealed class PrimitiveTypeSymbol : TypeSymbol
{
    public override Symbol? ContainingSymbol => null;

    public override string Name { get; }
    public override bool IsValueType { get; }
    public bool IsBaseType { get; }
    public TypeSymbol[] Bases { get; }

    public PrimitiveTypeSymbol(string name, bool isValueType, bool isBaseType = false, params TypeSymbol[] bases)
    {
        this.Name = name;
        this.IsValueType = isValueType;
        this.IsBaseType = isBaseType;
        this.Bases = bases;
    }

    public override string ToString() => this.Name;
}
