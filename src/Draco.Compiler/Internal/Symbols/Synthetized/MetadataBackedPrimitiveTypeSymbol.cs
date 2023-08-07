using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A primitive that's backed by a .NET metadata definition.
/// </summary>
internal sealed class MetadataBackedPrimitiveTypeSymbol : PrimitiveTypeSymbol
{
    public override IEnumerable<Symbol> DefinedMembers => this.metadataType.DefinedMembers;
    public override string Documentation => this.metadataType.Documentation;
    public override ImmutableArray<TypeSymbol> ImmediateBaseTypes => this.metadataType.ImmediateBaseTypes;

    private readonly TypeSymbol metadataType;

    public MetadataBackedPrimitiveTypeSymbol(string name, bool isValueType, TypeSymbol metadataType)
        : base(name, isValueType)
    {
        this.metadataType = metadataType;
    }
}
