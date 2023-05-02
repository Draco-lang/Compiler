using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A synthetized constructor function for metadata types.
/// </summary>
internal sealed class SynthetizedMetadataConstructorSymbol : SynthetizedFunctionSymbol
{
    public SynthetizedMetadataConstructorSymbol(MetadataTypeSymbol containingType, MethodDefinition ctorDefinition)
        : base(containingType.Name)
    {
    }
}
