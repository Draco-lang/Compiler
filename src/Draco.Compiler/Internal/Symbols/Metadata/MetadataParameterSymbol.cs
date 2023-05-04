using System.Linq;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A parameter read up from metadata.
/// </summary>
internal sealed class MetadataParameterSymbol : ParameterSymbol, IMetadataSymbol
{
    public override string Name => this.MetadataName;
    public string MetadataName => this.MetadataReader.GetString(this.parameterDefinition.Name);

    public override TypeSymbol Type { get; }
    public override Symbol ContainingSymbol { get; }

    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly Parameter parameterDefinition;

    public MetadataParameterSymbol(Symbol containingSymbol, Parameter parameterDefinition, TypeSymbol type)
    {
        this.ContainingSymbol = containingSymbol;
        this.Type = type;
        this.parameterDefinition = parameterDefinition;
    }
}
