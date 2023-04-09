using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Since we are treating constructors are regular methods, we need to synthetize
/// constructors from metadata. This symbol represents one of these synthetized
/// constructor functions.
/// </summary>
internal sealed class SynthetizedMetadataConstructorSymbol : MetadataMethodSymbol
{
    public override TypeSymbol ReturnType => (TypeSymbol)this.ContainingSymbol;
    public override string Name => this.ContainingSymbol.Name;

    public SynthetizedMetadataConstructorSymbol(
        MetadataTypeSymbol containingType,
        MethodDefinition methodDefinition,
        MetadataReader metadataReader)
        : base(containingType, methodDefinition, metadataReader)
    {
    }
}
