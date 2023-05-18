namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Represents a type of some type-checking error. Acts as a sentinel value, absorbs cascading errors.
/// </summary>
internal sealed class ErrorTypeSymbol : TypeSymbol
{
    public override bool IsError => true;
    public override Symbol? ContainingSymbol => null;

    /// <summary>
    /// The display name of the type.
    /// </summary>
    public string DisplayName { get; }

    public ErrorTypeSymbol(string name)
    {
        this.DisplayName = name;
    }

    public override string ToString() => this.DisplayName;
}
