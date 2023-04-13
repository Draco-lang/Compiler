using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A module merging all metadata references in the compilation.
/// </summary>
internal sealed class MetadataReferencesModuleSymbol : ModuleSymbol
{
    public override IEnumerable<Symbol> Members
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.members!.Value;
        }
    }
    private ImmutableArray<Symbol>? members;

    /// <summary>
    /// All metadata assemblies the compilation references.
    /// </summary>
    public ImmutableArray<MetadataAssemblySymbol> MetadataAssemblies
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.metadataAssemblies!.Value;
        }
    }
    private ImmutableArray<MetadataAssemblySymbol>? metadataAssemblies;

    public override Symbol? ContainingSymbol => null;

    private bool NeedsBuild => this.members is null;

    // NOTE: This is NOT declaring compilation
    private readonly Compilation compilation;

    public MetadataReferencesModuleSymbol(Compilation compilation)
    {
        this.compilation = compilation;
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();

    private void Build()
    {
        // Member list
        var members = ImmutableArray.CreateBuilder<Symbol>();
        // Metadata assemblies
        var metadataAssemblies = ImmutableArray.CreateBuilder<MetadataAssemblySymbol>();
        // Submodules found
        var submodules = new List<ModuleSymbol>();
        // Symbols left for processing
        var worklist = new Queue<Symbol>();

        // Add metadata reference modules
        foreach (var metadataReference in this.compilation.MetadataReferences)
        {
            var reader = metadataReference.MetadataReader;
            // Create the assembly
            var assemblySymbol = new MetadataAssemblySymbol(this, this.compilation, reader);
            // Add it to the worklist for further processing
            worklist.Enqueue(assemblySymbol);
            // Save it into the result list
            metadataAssemblies.Add(assemblySymbol);
        }

        while (worklist.TryDequeue(out var symbol))
        {
            if (symbol is MetadataAssemblySymbol assemblySymbol)
            {
                // This is an assembly from metadata, we need its root namespace
                foreach (var member in assemblySymbol.RootNamespace.Members) worklist.Enqueue(member);
            }
            else if (symbol is ModuleSymbol moduleSymbol)
            {
                // If the module has no name, we inline it, otherwise we add it as submodule
                if (string.IsNullOrWhiteSpace(moduleSymbol.Name))
                {
                    foreach (var member in moduleSymbol.Members) worklist.Enqueue(member);
                }
                else
                {
                    submodules.Add(moduleSymbol);
                }
            }
            else
            {
                // Any other symbol
                members.Add(symbol);
            }
        }

        // Group submodules by name
        var submodulesByName = submodules.GroupBy(m => m.Name);
        // And add them as merged modules
        foreach (var group in submodulesByName)
        {
            var groupElements = group.ToImmutableArray();
            // For single-element groups we skip merging
            if (groupElements.Length == 1) members.Add(groupElements[0]);
            else members.Add(new MergedModuleSymbol(this, groupElements));
        }

        // Save results
        this.members = members.ToImmutable();
        this.metadataAssemblies = metadataAssemblies.ToImmutable();
    }
}
