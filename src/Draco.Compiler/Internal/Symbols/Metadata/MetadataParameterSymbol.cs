using System.Linq;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A parameter read up from metadata.
/// </summary>
internal sealed class MetadataParameterSymbol : ParameterSymbol
{
    public override string Name => this.MetadataReader.GetString(this.parameterDefinition.Name);

    public override TypeSymbol Type { get; }
    public override Symbol ContainingSymbol { get; }

    /// <summary>
    /// The metadata assembly of this metadata symbol.
    /// </summary>
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    /// <summary>
    /// The metadata reader that was used to read up this metadata symbol.
    /// </summary>
    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly Parameter parameterDefinition;

    public MetadataParameterSymbol(Symbol containingSymbol, Parameter parameterDefinition, TypeSymbol type)
    {
        this.ContainingSymbol = containingSymbol;
        this.Type = type;
        this.parameterDefinition = parameterDefinition;
    }
}
