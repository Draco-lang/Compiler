using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
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
        foreach (var methodHandle in this.metadataReader.MethodDefinitions)
        {
            var methodDef = this.metadataReader.GetMethodDefinition(methodHandle);
            // Skip methods with special name
            if (methodDef.Attributes.HasFlag(MethodAttributes.SpecialName)) continue;
            // Skip non-public methods
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Public)) continue;
            // Skip non-static methods
            // TODO: What's Invoke in System.Console?
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Static)) continue;
            var methodSym = new MetadataStaticMethodSymbol(
                containingSymbol: this,
                methodDefinition: methodDef,
                metadataReader: this.metadataReader);
            // NOTE: We temporarily filter out elements we don't support yet
            // For that we enforce decoding the signature and catch the signaling exception
            try
            {
                // Enforce evaluation of signature
                _ = methodSym.Parameters;
                _ = methodSym.ReturnType;
            }
            catch (UnsupportedMetadataException)
            {
                // Bail
                continue;
            }
            result.Add(methodSym);
        }

        // Done
        return result.ToImmutable();
    }
}
