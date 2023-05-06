using System;
using System.Linq;
using System.Reflection.Metadata;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Metadata;

internal sealed class MetadataFieldSymbol : GlobalSymbol
{
    public override TypeSymbol Type => this.type ??= this.Build();

    private TypeSymbol? type;

    public override bool IsMutable => false; // TODO

    public override string Name => this.MetadataReader.GetString(this.fieldDefinition.Name);

    public override Symbol? ContainingSymbol { get; }

    /// <summary>
    /// The metadata assembly of this metadata symbol.
    /// </summary>
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    /// <summary>
    /// The metadata reader that was used to read up this metadata symbol.
    /// </summary>
    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly FieldDefinition fieldDefinition;
    public MetadataFieldSymbol(Symbol containingSymbol, FieldDefinition fieldDefinition)
    {
        this.ContainingSymbol = containingSymbol;
        this.fieldDefinition = fieldDefinition;
    }

    private TypeSymbol Build()
    {
        // Decode signature
        var decoder = new SignatureDecoder(this.Assembly.Compilation);
        return this.fieldDefinition.DecodeSignature(decoder, default);
    }
}
