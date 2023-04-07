using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A module imported from metadata.
/// </summary>
internal class MetadataModuleSymbol : ModuleSymbol
{
    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override string Name => this.metadataReader.GetString(this.moduleDefinition.Name);
    public override Symbol? ContainingSymbol => null;

    private readonly ModuleDefinition moduleDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataModuleSymbol(MetadataReader metadataReader)
    {
        this.metadataReader = metadataReader;
        this.moduleDefinition = metadataReader.GetModuleDefinition();
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();

    private ImmutableArray<Symbol> BuildMembers()
    {
        var rootNamespaceDefinition = this.metadataReader.GetNamespaceDefinitionRoot();
        var rootNamespaceSymbol = new MetadataNamespaceSymbol(
            containingSymbol: this,
            namespaceDefinition: rootNamespaceDefinition,
            metadataReader: this.metadataReader);
        return ImmutableArray.Create<Symbol>(rootNamespaceSymbol);
    }
}
