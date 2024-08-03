namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A type parameter synthetized by the compiler.
/// </summary>
internal sealed class SynthetizedTypeParameterSymbol(
    Symbol? containingSymbol,
    string name) : TypeParameterSymbol
{
    public override Symbol? ContainingSymbol { get; } = containingSymbol;

    public override string Name { get; } = name;
}
