using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Api;

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

    public override Compilation DeclaringCompilation { get; }

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly NamespaceDefinition namespaceDefinition;

    public MetadataNamespaceSymbol(Symbol containingSymbol, NamespaceDefinition namespaceDefinition, Compilation declaringCompilation)
    {
        this.ContainingSymbol = containingSymbol;
        this.namespaceDefinition = namespaceDefinition;
        this.DeclaringCompilation = declaringCompilation;
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
                namespaceDefinition: subNamespaceDef,
                declaringCompilation: this.DeclaringCompilation);
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
            var symbols = MetadataSymbol.ToSymbol(this, typeDef, this.MetadataReader, this.DeclaringCompilation);
            result.AddRange(symbols);
        }

        // Done
        return result.ToImmutable();
    }
}
