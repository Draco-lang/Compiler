namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A compiler-generated this parameter.
/// </summary>
internal sealed class SynthetizedThisParameterSymbol(FunctionSymbol containingSymbol) : ParameterSymbol
{
    public override FunctionSymbol ContainingSymbol => containingSymbol;
    public override TypeSymbol Type => (TypeSymbol)containingSymbol.ContainingSymbol!;
    public override bool IsThis => true;
}
