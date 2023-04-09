using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A module merging all of the root compilation modules.
/// </summary>
internal sealed class RootModuleSymbol : ModuleSymbol
{
    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override Symbol? ContainingSymbol => null;

    // NOTE: This is NOT declaring compilation
    private readonly Compilation compilation;

    public RootModuleSymbol(Compilation compilation)
    {
        this.compilation = compilation;
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();

    private ImmutableArray<Symbol> BuildMembers()
    {
        // Result list
        var result = ImmutableArray.CreateBuilder<Symbol>();
        // Submodules found
        var submodules = new List<ModuleSymbol>();
        // Symbols left for processing
        var worklist = new Queue<Symbol>();
        // Add source module
        worklist.Enqueue(this.compilation.SourceModule);
        // Add metadata reference modules
        foreach (var metadataReference in this.compilation.MetadataReferences)
        {
            var reader = metadataReference.MetadataReader;
            // Create the assembly
            var assemblySymbol = new MetadataAssemblySymbol(this, reader);
            // Add it
            worklist.Enqueue(assemblySymbol);
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
                result.Add(symbol);
            }
        }

        // Group submodules by name
        var submodulesByName = submodules.GroupBy(m => m.Name);
        // And add them as merged modules
        foreach (var group in submodulesByName)
        {
            var groupElements = group.ToImmutableArray();
            // For single-element groups we skip merging
            if (groupElements.Length == 1) result.Add(groupElements[0]);
            else result.Add(new MergedModuleSymbol(this, groupElements));
        }

        return result.ToImmutable();
    }
}
