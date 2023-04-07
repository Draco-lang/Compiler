using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A type definition read up from metadata.
/// </summary>
internal sealed class MetadataTypeSymbol : TypeSymbol
{
    public override string Name => this.metadataReader.GetString(this.typeDefinition.Name);
    public override Types.Type Type => throw new NotImplementedException();
    public override Symbol ContainingSymbol { get; }

    private readonly TypeDefinition typeDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataTypeSymbol(
        Symbol containingSymbol,
        TypeDefinition typeDefinition,
        MetadataReader metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
        this.typeDefinition = typeDefinition;
        this.metadataReader = metadataReader;
    }
}
