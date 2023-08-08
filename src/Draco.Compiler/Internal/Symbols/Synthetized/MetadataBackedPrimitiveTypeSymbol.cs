using System.Collections.Generic;
using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

// TODO: Don't we want to just merge the two at this point?
// Or at least make this type derived from the metadata version to be compatible?
// I don't know... Maybe separation for this case might be better overall.
/// <summary>
/// A primitive that's backed by a .NET metadata definition.
/// </summary>
internal sealed class MetadataBackedPrimitiveTypeSymbol : PrimitiveTypeSymbol
{
    /// <summary>
    /// The backing metadata type.
    /// </summary>
    public TypeSymbol MetadataType { get; }

    public override ImmutableArray<TypeSymbol> ImmediateBaseTypes => this.MetadataType.ImmediateBaseTypes;
    public override IEnumerable<Symbol> DefinedMembers => this.MetadataType.DefinedMembers;
    public override string Documentation => this.MetadataType.Documentation;

    public MetadataBackedPrimitiveTypeSymbol(string name, bool isValueType, TypeSymbol metadataType)
        : base(name, isValueType)
    {
        this.MetadataType = metadataType;
    }
}
