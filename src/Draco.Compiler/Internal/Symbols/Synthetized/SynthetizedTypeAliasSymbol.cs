namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A type-alias defined by the compiler.
/// </summary>
internal sealed class SynthetizedTypeAliasSymbol(string name, TypeSymbol substitution) : TypeAliasSymbol
{
    public override string Name { get; } = name;
    public override TypeSymbol Substitution { get; } = substitution;
}
