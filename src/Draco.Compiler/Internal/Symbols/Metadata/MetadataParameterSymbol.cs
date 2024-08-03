using System.Linq;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A parameter read up from metadata.
/// </summary>
internal sealed class MetadataParameterSymbol(
    FunctionSymbol containingSymbol,
    Parameter parameterDefinition,
    TypeSymbol type) : ParameterSymbol, IMetadataSymbol
{
    public override string Name => this.MetadataName;
    public override string MetadataName => this.MetadataReader.GetString(parameterDefinition.Name);

    public override TypeSymbol Type { get; } = type;
    public override FunctionSymbol ContainingSymbol { get; } = containingSymbol;

    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;
}
