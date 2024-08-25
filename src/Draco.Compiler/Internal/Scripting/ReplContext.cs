using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Scripting;

/// <summary>
/// Represents the execution context of the REPL.
/// This is a mutable class, keeping track of state and evolving with the REPL session.
/// </summary>
internal sealed class ReplContext
{
    /// <summary>
    /// The global imports for the context to be used in the compilation.
    /// </summary>
    public GlobalImports GlobalImports => new(
        this.globalImports.ToImmutable(),
        this.globalAliases.ToImmutable());

    /// <summary>
    /// The metadata references for the context to be used in the compilation.
    /// </summary>
    public ImmutableArray<MetadataReference> MetadataReferences => this.metadataReferences.ToImmutable();

    // Symbols and imports
    private readonly ImmutableArray<string>.Builder globalImports = ImmutableArray.CreateBuilder<string>();
    private readonly ImmutableArray<(string Name, string FullPath)>.Builder globalAliases
        = ImmutableArray.CreateBuilder<(string Name, string FullPath)>();

    // Metadata references
    private readonly ImmutableArray<MetadataReference>.Builder metadataReferences
        = ImmutableArray.CreateBuilder<MetadataReference>();

    // Assembly loading
    private readonly AssemblyLoadContext assemblyLoadContext;
    private readonly Dictionary<string, Assembly> loadedAssemblies = [];

    public ReplContext()
    {
        this.assemblyLoadContext = new AssemblyLoadContext(null, isCollectible: true);
        this.assemblyLoadContext.Resolving += this.LoadContextResolving;
    }

    /// <summary>
    /// Adds a global import to the context.
    /// </summary>
    /// <param name="path">The import path to add.</param>
    public void AddImport(string path) => this.globalImports.Add(path);

    /// <summary>
    /// Adds a set of global imports to the context.
    /// </summary>
    /// <param name="globalImports">The global imports to add.</param>
    public void AddAll(GlobalImports globalImports)
    {
        this.globalImports.AddRange(globalImports.ModuleImports);

        foreach (var alias in globalImports.ImportAliases)
        {
            // We remove shadowed symbols
            // NOTE: For simplicity and to not cross compilation borders, we remove by name
            this.globalAliases.RemoveAll(s => s.Name == alias.Name);

            // Add the new symbol
            this.globalAliases.Add(alias);
        }
    }

    /// <summary>
    /// Adds a metadata reference to the context.
    /// </summary>
    /// <param name="metadataReference">The metadata reference to add.</param>
    public void AddMetadataReference(MetadataReference metadataReference) =>
        this.metadataReferences.Add(metadataReference);

    /// <summary>
    /// Loads the given assembly into the memory context.
    /// </summary>
    /// <param name="stream">The stream to load the assembly from.</param>
    /// <returns>The loaded assembly.</returns>
    public Assembly LoadAssembly(Stream stream)
    {
        var assembly = this.assemblyLoadContext.LoadFromStream(stream);
        var assemblyName = assembly.GetName().Name;
        // TODO: This is a bad way to compare assemblies
        if (assemblyName is not null) this.loadedAssemblies.Add(assemblyName, assembly);
        return assembly;
    }

    private Assembly? LoadContextResolving(AssemblyLoadContext context, AssemblyName name)
    {
        if (name.Name is null) return null;
        // TODO: This is a bad way to compare assemblies
        return this.loadedAssemblies.TryGetValue(name.Name, out var assembly) ? assembly : null;
    }
}
