namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a type of some type-checking error. Acts as a sentinel value, absorbs cascading errors.
/// </summary>
internal sealed class ErrorTypeSymbol : TypeSymbol
{
    public override bool IsError => true;

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
