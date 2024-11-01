namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// An error this parameter symbol.
/// </summary>
/// <param name="type"></param>
/// <param name="containingSymbol"></param>
internal sealed class ErrorThisParameterSymbol(TypeSymbol type, FunctionSymbol containingSymbol) : ParameterSymbol
{
    public override TypeSymbol Type { get; } = type;
    public override FunctionSymbol ContainingSymbol { get; } = containingSymbol;
}
