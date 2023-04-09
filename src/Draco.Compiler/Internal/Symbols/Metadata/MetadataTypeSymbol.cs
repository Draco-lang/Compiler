using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A type definition read up from metadata.
/// </summary>
internal sealed class MetadataTypeSymbol : TypeSymbol
{
    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override string Name => this.metadataReader.GetString(this.typeDefinition.Name);
    public override Symbol ContainingSymbol { get; }
    // TODO: Is this correct?
    public bool IsValueType => !this.typeDefinition.Attributes.HasFlag(TypeAttributes.Class);

    private readonly TypeDefinition typeDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataTypeSymbol(
        Symbol containingSymbol,
        TypeDefinition typeDefinition,
        MetadataReader metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
        this.typeDefinition = typeDefinition;
        this.metadataReader = metadataReader;
    }

    public override string ToString() => this.Name;

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // TODO: nested-types
        // TODO: static fields
        // TODO: static properties
        // TODO: nonstatic fields
        // TODO: nonstatic properties

        // Methods
        foreach (var methodHandle in this.typeDefinition.GetMethods())
        {
            var method = this.metadataReader.GetMethodDefinition(methodHandle);
            // Skip private
            if (method.Attributes.HasFlag(MethodAttributes.Private)) continue;
            // Skip special name
            if (method.Attributes.HasFlag(MethodAttributes.SpecialName)) continue;
            // Add it
            var methodSymbol = new MetadataMethodSymbol(
                containingSymbol: this,
                methodDefinition: method,
                metadataReader: this.metadataReader);
            result.Add(methodSymbol);
        }

        // Done
        return result.ToImmutable();
    }
}
