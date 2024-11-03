namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// A 'this' parameter symbol where it's invalid.
/// </summary>
internal sealed class ErrorThisParameterSymbol(
    TypeSymbol type,
    FunctionSymbol containingSymbol) : ParameterSymbol
{
    public override TypeSymbol Type { get; } = type;
    public override FunctionSymbol ContainingSymbol { get; } = containingSymbol;

    public override bool IsError => true;
}
