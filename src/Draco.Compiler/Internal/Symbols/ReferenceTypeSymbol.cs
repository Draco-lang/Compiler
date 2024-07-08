namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a reference type.
/// </summary>
internal sealed class ReferenceTypeSymbol(TypeSymbol elementType) : TypeSymbol
{
    /// <summary>
    /// The element type that the reference references to.
    /// </summary>
    public TypeSymbol ElementType { get; } = elementType;

    public override bool IsValueType => true;

    public override string ToString() => $"ref {this.ElementType}";
}
