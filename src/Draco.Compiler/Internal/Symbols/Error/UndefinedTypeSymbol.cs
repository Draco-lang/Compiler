namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an undefined, in-source type reference.
/// </summary>
internal sealed class UndefinedTypeSymbol(string name) : TypeSymbol
{
    public override bool IsError => true;

    public override string Name { get; } = name;

    public override string ToString() => this.Name;
}
