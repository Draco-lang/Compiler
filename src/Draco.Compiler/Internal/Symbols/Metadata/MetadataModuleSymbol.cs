using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A module imported from metadata.
/// </summary>
internal sealed class MetadataModuleSymbol : ModuleSymbol
{
    public override string Name => this.metadataReader.GetString(this.moduleDefinition.Name);
    public override Symbol? ContainingSymbol => null;

    private readonly ModuleDefinition moduleDefinition;
    private readonly NamespaceDefinition rootNamespaceDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataModuleSymbol(MetadataReader metadataReader)
    {
        this.metadataReader = metadataReader;
        this.moduleDefinition = metadataReader.GetModuleDefinition();
        this.rootNamespaceDefinition = metadataReader.GetNamespaceDefinitionRoot();
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();
}
