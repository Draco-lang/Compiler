namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A parameter constructed by the compiler.
/// </summary>
internal sealed class SynthetizedParameterSymbol : ParameterSymbol
{
    public override TypeSymbol Type { get; }

    public override Symbol? ContainingSymbol => null;

    public SynthetizedParameterSymbol(TypeSymbol type)
    {
        this.Type = type;
    }
}
