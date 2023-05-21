namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A parameter constructed by the compiler.
/// </summary>
internal sealed class SynthetizedParameterSymbol : ParameterSymbol
{
    public override string Name { get; }
    public override TypeSymbol Type { get; }

    public override Symbol? ContainingSymbol { get; }

    public SynthetizedParameterSymbol(Symbol? containingSymbol, string name, TypeSymbol type)
    {
        this.ContainingSymbol = containingSymbol;
        this.Name = name;
        this.Type = type;
    }

    public SynthetizedParameterSymbol(Symbol? containingSymbol, TypeSymbol type)
        : this(containingSymbol, string.Empty, type)
    {
    }
}
