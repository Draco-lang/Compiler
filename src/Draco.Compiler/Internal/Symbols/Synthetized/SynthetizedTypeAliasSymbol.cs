namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A type-alias defined by the compiler.
/// </summary>
internal sealed class SynthetizedTypeAliasSymbol : TypeAliasSymbol
{
    public override string Name { get; }
    public override Symbol? ContainingSymbol => null;
    public override TypeSymbol Substitution { get; }

    public SynthetizedTypeAliasSymbol(string name, TypeSymbol substitution)
    {
        this.Name = name;
        this.Substitution = substitution;
    }
}
