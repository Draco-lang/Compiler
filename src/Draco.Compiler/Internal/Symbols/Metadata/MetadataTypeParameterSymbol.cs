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
internal sealed class MetadataTypeParameterSymbol : TypeParameterSymbol
{
    public override string Name => this.MetadataReader.GetString(this.genericParameter.Name);

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

    private readonly GenericParameter genericParameter;

    public MetadataTypeParameterSymbol(Symbol containingSymbol, GenericParameter genericParameter)
    {
        this.ContainingSymbol = containingSymbol;
        this.genericParameter = genericParameter;
    }
}
