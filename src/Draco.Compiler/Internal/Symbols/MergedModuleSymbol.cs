using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a module with a single name, merged from multiple sources.
/// </summary>
internal sealed class MergedModuleSymbol : ModuleSymbol
{
    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override Symbol? ContainingSymbol { get; }
    public override string Name { get; }

    private readonly ImmutableArray<ModuleSymbol> modules;

    public MergedModuleSymbol(Symbol? containingSymbol, ImmutableArray<ModuleSymbol> modules)
    {
        Debug.Assert(modules.All(m => m.Name == modules[0].Name));

        this.ContainingSymbol = containingSymbol;
        this.Name = modules[0].Name;
        this.modules = modules;
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();

    private ImmutableArray<Symbol> BuildMembers()
    {
        var members = ImmutableArray.CreateBuilder<Symbol>();
        var submodules = new List<ModuleSymbol>();
        foreach (var singleModule in this.modules)
        {
            // singleModule is a piece of this module, we need to go through each element of that
            foreach (var member in singleModule.Members)
            {
                if (member is ModuleSymbol module)
                {
                    submodules.Add(module);
                }
                else
                {
                    members.Add(member);
                }
            }
        }
        // We need to merge submodules by name
        var submodulesGrouped = submodules.GroupBy(m => m.Name);
        // And add them as merged modules
        foreach (var group in submodulesGrouped)
        {
            var groupElements = group.ToImmutableArray();
            // For single-element groups we skip merging
            if (groupElements.Length == 1) members.Add(groupElements[0]);
            else members.Add(new MergedModuleSymbol(this, groupElements));
        }
        return members.ToImmutable();
    }
}
