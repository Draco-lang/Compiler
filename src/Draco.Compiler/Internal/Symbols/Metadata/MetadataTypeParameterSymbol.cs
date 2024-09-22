using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A type parameter read up from metadata.
/// </summary>
internal sealed class MetadataTypeParameterSymbol(
    Symbol containingSymbol,
    GenericParameter genericParameter) : TypeParameterSymbol, IMetadataSymbol
{
    public override Compilation DeclaringCompilation => this.Assembly.DeclaringCompilation;

    public override ImmutableArray<AttributeInstance> Attributes => InterlockedUtils.InitializeDefault(ref this.attributes, this.BuildAttributes);
    private ImmutableArray<AttributeInstance> attributes;

    public override string Name => this.MetadataName;
    public override string MetadataName => this.MetadataReader.GetString(genericParameter.Name);

    public override Symbol ContainingSymbol { get; } = containingSymbol;

    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private ImmutableArray<AttributeInstance> BuildAttributes() =>
        MetadataSymbol.DecodeAttributeList(genericParameter.GetCustomAttributes(), this);
}
