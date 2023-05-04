using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A type parameter read up from metadata.
/// </summary>
internal sealed class MetadataTypeParameterSymbol : TypeParameterSymbol, IMetadataSymbol
{
    public override string Name => this.MetadataName;
    public string MetadataName => this.MetadataReader.GetString(this.genericParameter.Name);

    public override Symbol ContainingSymbol { get; }

    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly GenericParameter genericParameter;

    public MetadataTypeParameterSymbol(Symbol containingSymbol, GenericParameter genericParameter)
    {
        this.ContainingSymbol = containingSymbol;
        this.genericParameter = genericParameter;
    }
}
