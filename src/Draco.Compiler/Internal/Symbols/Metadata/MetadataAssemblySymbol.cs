using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using System.Xml;
using Draco.Compiler.Api;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// An assembly imported from metadata.
/// </summary>
internal class MetadataAssemblySymbol(
    Compilation compilation,
    MetadataReader metadataReader,
    XmlDocument? documentation) : ModuleSymbol, IMetadataSymbol
{
    public override IEnumerable<Symbol> Members => this.RootNamespace.Members;

    /// <summary>
    /// The version of this assembly.
    /// </summary>
    public Version Version => this.assemblyDefinition.Version;

    /// <summary>
    /// The root namespace of this assembly.
    /// </summary>
    public MetadataNamespaceSymbol RootNamespace =>
        LazyInitializer.EnsureInitialized(ref this.rootNamespace, this.BuildRootNamespace);
    private MetadataNamespaceSymbol? rootNamespace;

    public override string Name => this.MetadataName;
    // NOTE: We don't emit the name of the module in fully qualified names
    public override string FullName => string.Empty;

    /// <summary>
    /// The <see cref="System.Reflection.AssemblyName"/> of this referenced assembly.
    /// </summary>
    public AssemblyName AssemblyName =>
        LazyInitializer.EnsureInitialized(ref this.assemblyName, this.assemblyDefinition.GetAssemblyName);
    private AssemblyName? assemblyName;

    public override string MetadataName => this.MetadataReader.GetString(this.assemblyDefinition.Name);
    public MetadataAssemblySymbol Assembly => this;

    public MetadataReader MetadataReader { get; } = metadataReader;

    /// <summary>
    /// XmlDocument containing documentation for this assembly.
    /// </summary>
    public XmlDocument? AssemblyDocumentation { get; } = documentation;

    /// <summary>
    /// The compilation this assembly belongs to.
    /// </summary>
    public Compilation Compilation { get; } = compilation;

    private readonly ModuleDefinition moduleDefinition = metadataReader.GetModuleDefinition();
    private readonly AssemblyDefinition assemblyDefinition = metadataReader.GetAssemblyDefinition();

    private MetadataNamespaceSymbol BuildRootNamespace()
    {
        var rootNamespaceDefinition = this.MetadataReader.GetNamespaceDefinitionRoot();
        return new MetadataNamespaceSymbol(
            containingSymbol: this,
            namespaceDefinition: rootNamespaceDefinition);
    }
}
