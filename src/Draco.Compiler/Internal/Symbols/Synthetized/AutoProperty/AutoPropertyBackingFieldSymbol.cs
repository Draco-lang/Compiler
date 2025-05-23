namespace Draco.Compiler.Internal.Symbols.Synthetized.AutoProperty;

/// <summary>
/// Auto-generated backing field for an auto-property.
/// </summary>
internal sealed class AutoPropertyBackingFieldSymbol(
    Symbol containingSymbol,
    PropertySymbol property) : FieldSymbol
{
    public override Symbol ContainingSymbol { get; } = containingSymbol;

    public override TypeSymbol Type => this.Property.Type;
    public override bool IsStatic => this.Property.IsStatic;
    public override bool IsMutable => this.Property.Setter is not null;
    public override string Name => $"<{this.Property.Name}>_BackingField";
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Private;
    public override bool IsSpecialName => true;

    /// <summary>
    /// The property this backing field is for.
    /// </summary>
    public PropertySymbol Property { get; } = property;
}
