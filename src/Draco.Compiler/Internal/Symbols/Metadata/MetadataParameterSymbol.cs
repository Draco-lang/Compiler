using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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

    public override Type Type { get; }
    public override Symbol ContainingSymbol { get; }

    private readonly Parameter parameterDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataParameterSymbol(
        Symbol containingSymbol,
        Parameter parameterDefinition,
        Type type,
        MetadataReader metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
        this.Type = type;
        this.parameterDefinition = parameterDefinition;
        this.metadataReader = metadataReader;
    }
}
