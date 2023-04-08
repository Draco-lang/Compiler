using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A static method read up from metadata.
/// </summary>
internal sealed class MetadataStaticMethodSymbol : MetadataMethodSymbol
{
    public override Symbol ContainingSymbol { get; }

    public MetadataStaticMethodSymbol(
        Symbol containingSymbol,
        MethodDefinition methodDefinition,
        MetadataReader metadataReader)
        : base(methodDefinition, metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
    }
}
