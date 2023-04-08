using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A type definition read up from metadata.
/// </summary>
internal sealed class MetadataTypeSymbol : TypeSymbol
{
    public override string Name => this.metadataReader.GetString(this.typeDefinition.Name);
    public override Symbol ContainingSymbol { get; }
    // TODO: Is this correct?
    public bool IsValueType => !this.typeDefinition.Attributes.HasFlag(TypeAttributes.Class);

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

    public override string ToString() => this.Name;
}
