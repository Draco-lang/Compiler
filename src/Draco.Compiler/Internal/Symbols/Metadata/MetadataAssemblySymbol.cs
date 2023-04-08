using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// An assembly imported from metadata.
/// </summary>
internal class MetadataAssemblySymbol : ModuleSymbol
{
    public override IEnumerable<Symbol> Members => new[] { this.RootNamespace };

    public MetadataNamespaceSymbol RootNamespace => this.rootNamespace ??= this.BuildRootNamespace();
    private MetadataNamespaceSymbol? rootNamespace;

    public override string Name => this.metadataReader.GetString(this.assemblyDefinition.Name);
    // NOTE: We don't emit the name of the module in fully qualified names
    public override string FullName => string.Empty;
    public override Symbol? ContainingSymbol => null;

    private readonly ModuleDefinition moduleDefinition;
    private readonly AssemblyDefinition assemblyDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataAssemblySymbol(MetadataReader metadataReader)
    {
        this.metadataReader = metadataReader;
        this.moduleDefinition = metadataReader.GetModuleDefinition();
        this.assemblyDefinition = metadataReader.GetAssemblyDefinition();
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();

    private MetadataNamespaceSymbol BuildRootNamespace()
    {
        var rootNamespaceDefinition = this.metadataReader.GetNamespaceDefinitionRoot();
        return new MetadataNamespaceSymbol(
            containingSymbol: this,
            namespaceDefinition: rootNamespaceDefinition,
            metadataReader: this.metadataReader);
    }
}
