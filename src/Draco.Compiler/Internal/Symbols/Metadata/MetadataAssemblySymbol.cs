using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Api;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// An assembly imported from metadata.
/// </summary>
internal class MetadataAssemblySymbol : ModuleSymbol, IMetadataSymbol
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
        InterlockedUtils.InitializeNull(ref this.rootNamespace, this.BuildRootNamespace);
    private MetadataNamespaceSymbol? rootNamespace;

    public override string Name => this.MetadataName;
    // NOTE: We don't emit the name of the module in fully qualified names
    public override string FullName => string.Empty;
    public override Symbol? ContainingSymbol => null;

    /// <summary>
    /// The <see cref="System.Reflection.AssemblyName"/> of this referenced assembly.
    /// </summary>
    public AssemblyName AssemblyName =>
        InterlockedUtils.InitializeNull(ref this.assemblyName, this.assemblyDefinition.GetAssemblyName);
    private AssemblyName? assemblyName;

    public override string MetadataName => this.MetadataReader.GetString(this.assemblyDefinition.Name);
    public MetadataAssemblySymbol Assembly => this;

    public MetadataReader MetadataReader { get; }

    /// <summary>
    /// The compilation this assembly belongs to.
    /// </summary>
    public Compilation Compilation { get; }

    private readonly ModuleDefinition moduleDefinition;
    private readonly AssemblyDefinition assemblyDefinition;

    public MetadataAssemblySymbol(
        Compilation compilation,
        MetadataReader metadataReader)
    {
        this.Compilation = compilation;
        this.MetadataReader = metadataReader;
        this.moduleDefinition = metadataReader.GetModuleDefinition();
        this.assemblyDefinition = metadataReader.GetAssemblyDefinition();
    }

    private MetadataNamespaceSymbol BuildRootNamespace()
    {
        using var _ = this.Compilation.TraceBegin($"MetadataAssemblySymbol({this.Name}).BuildRootNamespace");

        var rootNamespaceDefinition = this.MetadataReader.GetNamespaceDefinitionRoot();
        return new MetadataNamespaceSymbol(
            containingSymbol: this,
            namespaceDefinition: rootNamespaceDefinition);
    }
}
