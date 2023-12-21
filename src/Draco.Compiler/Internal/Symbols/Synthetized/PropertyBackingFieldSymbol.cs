using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Auto-generated backing field for an auto-property.
/// </summary>
internal sealed class PropertyBackingFieldSymbol : FieldSymbol
{
    public override TypeSymbol ContainingSymbol { get; }

    public override TypeSymbol Type => this.Property.Type;
    public override bool IsMutable => this.Property.Setter is not null;
    public override string Name => $"<{this.Property.Name}>_BackingField";
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Private;

    public PropertySymbol Property { get; }

    public PropertyBackingFieldSymbol(TypeSymbol containingSymbol, PropertySymbol property)
    {
        this.ContainingSymbol = containingSymbol;
        this.Property = property;
    }
}
