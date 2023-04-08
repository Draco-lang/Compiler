using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a module merged from multiple sources.
/// </summary>
internal sealed class MergedModuleSymbol : ModuleSymbol
{
    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override Symbol? ContainingSymbol { get; }

    private readonly ImmutableArray<ModuleSymbol> modules;

    public MergedModuleSymbol(Symbol? containingSymbol, ImmutableArray<ModuleSymbol> modules)
    {
        this.ContainingSymbol = containingSymbol;
        this.modules = modules;
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        foreach (var module in this.modules)
        {
            // TODO: We need to merge modules eventually

            if (module is MetadataAssemblySymbol metadataModule)
            {
                // We skip metadata modules, we only deal with the root namespace
                result.AddRange(metadataModule.RootNamespace.Members);
                continue;
            }

            // Add all members
            result.AddRange(module.Members);
        }

        return result.ToImmutable();
    }
}
