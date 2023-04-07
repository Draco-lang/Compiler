using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Source;

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

            if (module is MetadataModuleSymbol metadataModule)
            {
                // We skip metadata modules, we only deal with the root namespace
                result.Add(metadataModule.RootNamespace);
                continue;
            }

            // Add all members
            result.AddRange(module.Members);
        }

        return result.ToImmutable();
    }
}
