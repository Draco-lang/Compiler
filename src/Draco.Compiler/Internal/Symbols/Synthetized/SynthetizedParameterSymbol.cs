namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A parameter constructed by the compiler.
/// </summary>
internal sealed class SynthetizedParameterSymbol(
    FunctionSymbol containingSymbol,
    string name,
    TypeSymbol type) : ParameterSymbol
{
    public override string Name { get; } = name;
    public override TypeSymbol Type { get; } = type;

    public override FunctionSymbol ContainingSymbol { get; } = containingSymbol;

    public SynthetizedParameterSymbol(FunctionSymbol containingSymbol, TypeSymbol type)
        : this(containingSymbol, string.Empty, type)
    {
    }
}
