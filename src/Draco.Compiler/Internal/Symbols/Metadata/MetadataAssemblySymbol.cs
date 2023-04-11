using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// An assembly imported from metadata.
/// </summary>
internal class MetadataAssemblySymbol : ModuleSymbol
{
    public override IEnumerable<Symbol> Members => this.RootNamespace.Members;

    public MetadataNamespaceSymbol RootNamespace => this.rootNamespace ??= this.BuildRootNamespace();
    private MetadataNamespaceSymbol? rootNamespace;

    public override string Name => this.MetadataReader.GetString(this.assemblyDefinition.Name);
    // NOTE: We don't emit the name of the module in fully qualified names
    public override string FullName => string.Empty;
    public override Symbol ContainingSymbol { get; }

    /// <summary>
    /// The <see cref="System.Reflection.AssemblyName"/> of this referenced assembly.
    /// </summary>
    public AssemblyName AssemblyName => this.assemblyName ??= this.assemblyDefinition.GetAssemblyName();
    private AssemblyName? assemblyName;

    /// <summary>
    /// The metadata reader used to read this assembly.
    /// </summary>
    public MetadataReader MetadataReader { get; }

    /// <summary>
    /// The compilation this assembly belongs to.
    /// </summary>
    public Compilation Compilation { get; }

    private readonly ModuleDefinition moduleDefinition;
    private readonly AssemblyDefinition assemblyDefinition;

    public MetadataAssemblySymbol(
        Symbol containingSymbol,
        Compilation compilation,
        MetadataReader metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
        this.Compilation = compilation;
        this.MetadataReader = metadataReader;
        this.moduleDefinition = metadataReader.GetModuleDefinition();
        this.assemblyDefinition = metadataReader.GetAssemblyDefinition();
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();

    private MetadataNamespaceSymbol BuildRootNamespace()
    {
        var rootNamespaceDefinition = this.MetadataReader.GetNamespaceDefinitionRoot();
        return new MetadataNamespaceSymbol(
            containingSymbol: this,
            namespaceDefinition: rootNamespaceDefinition);
    }
}
