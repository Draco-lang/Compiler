namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A built-in primitive.
/// </summary>
internal sealed class PrimitiveTypeSymbol : TypeSymbol
{
    public override Symbol? ContainingSymbol => null;
    public override string Name { get; }
    public override bool IsValueType { get; }

    public PrimitiveTypeSymbol(string name, bool isValueType)
    {
        this.Name = name;
        this.IsValueType = isValueType;
    }

    public override string ToString() => this.Name;
}
