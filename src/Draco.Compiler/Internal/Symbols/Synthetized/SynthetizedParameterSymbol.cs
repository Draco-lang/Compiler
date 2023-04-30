namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A parameter constructed by the compiler.
/// </summary>
internal sealed class SynthetizedParameterSymbol : ParameterSymbol
{
    public override string Name { get; }
    public override TypeSymbol Type { get; }

    public override Symbol? ContainingSymbol => null;

    public SynthetizedParameterSymbol(string name, TypeSymbol type)
    {
        this.Name = name;
        this.Type = type;
    }

    public SynthetizedParameterSymbol(TypeSymbol type)
        : this(string.Empty, type)
    {
    }
}
