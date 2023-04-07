using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A parameter read up from metadata.
/// </summary>
internal sealed class MetadataParameterSymbol : ParameterSymbol
{
    public override string Name => this.metadataReader.GetString(this.parameterDefinition.Name);

    public override Type Type => throw new System.NotImplementedException();
    public override Symbol ContainingSymbol { get; }

    private readonly Parameter parameterDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataParameterSymbol(
        Symbol containingSymbol,
        Parameter parameterDefinition,
        MetadataReader metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
        this.parameterDefinition = parameterDefinition;
        this.metadataReader = metadataReader;
    }
}
