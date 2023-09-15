using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A namespace imported from metadata.
/// </summary>
internal sealed class MetadataNamespaceSymbol : ModuleSymbol, IMetadataSymbol
{
    public override IEnumerable<Symbol> Members =>
        InterlockedUtils.InitializeDefault(ref this.members, this.BuildMembers);
    private ImmutableArray<Symbol> members;

    public override string Name => this.MetadataName;
    public override string MetadataName => this.MetadataReader.GetString(this.namespaceDefinition.Name);
    public override Symbol ContainingSymbol { get; }

    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly NamespaceDefinition namespaceDefinition;

    public MetadataNamespaceSymbol(Symbol containingSymbol, NamespaceDefinition namespaceDefinition)
    {
        this.ContainingSymbol = containingSymbol;
        this.namespaceDefinition = namespaceDefinition;
    }

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // Sub-namespaces
        foreach (var subNamespaceHandle in this.namespaceDefinition.NamespaceDefinitions)
        {
            var subNamespaceDef = this.MetadataReader.GetNamespaceDefinition(subNamespaceHandle);
            var subNamespaceSym = new MetadataNamespaceSymbol(
                containingSymbol: this,
                namespaceDefinition: subNamespaceDef);
            result.Add(subNamespaceSym);
        }

        // Types
        foreach (var typeHandle in this.namespaceDefinition.TypeDefinitions)
        {
            var typeDef = this.MetadataReader.GetTypeDefinition(typeHandle);
            // Skip nested types, that will be handled by the type itself
            if (typeDef.IsNested) continue;
            // Skip types with special name
            if (typeDef.Attributes.HasFlag(TypeAttributes.SpecialName)) continue;
            // Skip non-public types
            if (!typeDef.Attributes.HasFlag(TypeAttributes.Public)) continue;
            // Turn into a symbol, or potentially symbols
            var symbols = MetadataSymbol.ToSymbol(this, typeDef, this.MetadataReader);
            result.AddRange(symbols);
        }

        // Done
        return result.ToImmutable();
    }

    /// <summary>
    /// Looks up symbol by its prefixed documentation name.
    /// </summary>
    /// <param name="prefixedDocumentationName">The prefixed documentation name to lookup by.</param>
    /// <returns>The looked up symbol, or null, if such symbol doesn't exist under this module symbol.</returns>
    public Symbol? LookupByPrefixedDocumentationName(string prefixedDocumentationName)
    {
        // Note: we cut off the first two chars, because the first two chars are always the prefix annotating what kind of symbol this is
        var parts = prefixedDocumentationName[2..].Split('.');
        if (parts.Length == 0) return this;

        var current = this as Symbol;
        for (var i = 0; i < parts.Length - 1; ++i)
        {
            var part = parts[i];
            current = current.Members
                .Where(m => m.MetadataName == part && m is ModuleSymbol or TypeSymbol)
                .SingleOrDefault();
            if (current is null) return null;
        }

        return current.Members.SingleOrDefault(m => MetadataSymbol.GetPrefixedDocumentationName(m) == prefixedDocumentationName);
    }
}
