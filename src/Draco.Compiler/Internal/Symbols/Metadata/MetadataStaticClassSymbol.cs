using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A static class read up from metadata that we handle as a module.
/// </summary>
internal sealed class MetadataStaticClassSymbol : ModuleSymbol, IMetadataSymbol
{
    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override string Name => this.MetadataName;
    public string MetadataName => this.MetadataReader.GetString(this.typeDefinition.Name);

    public override Symbol ContainingSymbol { get; }

    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly TypeDefinition typeDefinition;

    public MetadataStaticClassSymbol(Symbol containingSymbol, TypeDefinition typeDefinition)
    {
        this.ContainingSymbol = containingSymbol;
        this.typeDefinition = typeDefinition;
    }

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // TODO: nested-types
        // TODO: static fields
        // TODO: static properties

        // Methods
        foreach (var methodHandle in this.typeDefinition.GetMethods())
        {
            var methodDef = this.MetadataReader.GetMethodDefinition(methodHandle);
            // Skip methods with special name
            if (methodDef.Attributes.HasFlag(MethodAttributes.SpecialName)) continue;
            // Skip non-public methods
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Public)) continue;
            // Skip non-static methods
            // TODO: What's Invoke in System.Console?
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Static)) continue;
            var methodSym = new MetadataMethodSymbol(
                containingSymbol: this,
                methodDefinition: methodDef);
            result.Add(methodSym);
        }

        // Done
        return result.ToImmutable();
    }
}
