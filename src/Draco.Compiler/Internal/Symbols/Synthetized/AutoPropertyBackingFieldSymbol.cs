namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Auto-generated backing field for an auto-property.
/// </summary>
internal sealed class AutoPropertyBackingFieldSymbol(TypeSymbol containingSymbol, PropertySymbol property) : FieldSymbol
{
    public override TypeSymbol ContainingSymbol { get; } = containingSymbol;

    public override string Name => $"<{this.Property.Name}>_BackingField";
    public override TypeSymbol Type => this.Property.Type;
    public override bool IsMutable => this.Property.Setter is not null;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Private;

    /// <summary>
    /// The property this backing field is for.
    /// </summary>
    public PropertySymbol Property { get; } = property;
}
