using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A synthetized constructor function for metadata types.
/// </summary>
internal sealed class SynthetizedMetadataConstructorSymbol : SynthetizedFunctionSymbol
{
    public override MetadataTypeSymbol ContainingSymbol { get; }

    public override string Name => this.ContainingSymbol.Name;
    public override ImmutableArray<TypeParameterSymbol> GenericParameters => throw new NotImplementedException();
    public override ImmutableArray<ParameterSymbol> Parameters => throw new NotImplementedException();
    public override TypeSymbol ReturnType => throw new NotImplementedException();
    public override BoundStatement Body => throw new NotImplementedException();

    public SynthetizedMetadataConstructorSymbol(MetadataTypeSymbol containingType, MethodDefinition ctorDefinition)
    {
        this.ContainingSymbol = containingType;
    }
}
