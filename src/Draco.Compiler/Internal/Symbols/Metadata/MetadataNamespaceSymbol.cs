using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A namespace imported from metadata.
/// </summary>
internal sealed class MetadataNamespaceSymbol : ModuleSymbol
{
    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override string Name => this.metadataReader.GetString(this.namespaceDefinition.Name);
    public override Symbol ContainingSymbol { get; }

    private readonly NamespaceDefinition namespaceDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataNamespaceSymbol(
        Symbol containingSymbol,
        NamespaceDefinition namespaceDefinition,
        MetadataReader metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
        this.namespaceDefinition = namespaceDefinition;
        this.metadataReader = metadataReader;
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // Sub-namespaces
        foreach (var subNamespaceHandle in this.namespaceDefinition.NamespaceDefinitions)
        {
            var subNamespaceDef = this.metadataReader.GetNamespaceDefinition(subNamespaceHandle);
            var subNamespaceSym = new MetadataNamespaceSymbol(
                containingSymbol: this,
                namespaceDefinition: subNamespaceDef,
                metadataReader: this.metadataReader);
            result.Add(subNamespaceSym);
        }

        // TODO: Types

        // Done
        return result.ToImmutable();
    }
}
