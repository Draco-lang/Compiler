using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A static class read up from metadata that we handle as a module.
/// </summary>
internal sealed class MetadataStaticClassSymbol : ModuleSymbol
{
    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override string Name => this.metadataReader.GetString(this.typeDefinition.Name);

    public override Symbol ContainingSymbol { get; }

    private readonly TypeDefinition typeDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataStaticClassSymbol(
        Symbol containingSymbol,
        TypeDefinition typeDefinition,
        MetadataReader metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
        this.typeDefinition = typeDefinition;
        this.metadataReader = metadataReader;
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // TODO: nested-types
        // TODO: static fields
        // TODO: static properties

        // Methods
        foreach (var methodHandle in this.typeDefinition.GetMethods())
        {
            var methodDef = this.metadataReader.GetMethodDefinition(methodHandle);
            // Skip methods with special name
            if (methodDef.Attributes.HasFlag(MethodAttributes.SpecialName)) continue;
            // Skip non-public methods
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Public)) continue;
            // Skip non-static methods
            // TODO: What's Invoke in System.Console?
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Static)) continue;
            var methodSym = new MetadataMethodSymbol(
                containingSymbol: this,
                methodDefinition: methodDef,
                metadataReader: this.metadataReader);
            result.Add(methodSym);
        }

        // Done
        return result.ToImmutable();
    }
}
