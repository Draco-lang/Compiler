using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Draco.Compiler.Api;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A parameter read up from metadata.
/// </summary>
internal sealed class MetadataParameterSymbol(
    FunctionSymbol containingSymbol,
    Parameter parameterDefinition,
    TypeSymbol type) : ParameterSymbol, IMetadataSymbol
{
    public override Compilation DeclaringCompilation => this.Assembly.DeclaringCompilation;

    public override ImmutableArray<AttributeInstance> Attributes => InterlockedUtils.InitializeDefault(ref this.attributes, this.BuildAttributes);
    private ImmutableArray<AttributeInstance> attributes;

    public override string Name => this.MetadataName;
    public override string MetadataName => this.MetadataReader.GetString(parameterDefinition.Name);

    public override TypeSymbol Type { get; } = type;
    public override FunctionSymbol ContainingSymbol { get; } = containingSymbol;

    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private ImmutableArray<AttributeInstance> BuildAttributes() =>
        MetadataSymbol.DecodeAttributeList(parameterDefinition.GetCustomAttributes(), this);
}
