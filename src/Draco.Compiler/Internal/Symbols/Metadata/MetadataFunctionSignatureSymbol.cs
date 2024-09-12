using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// This symbol is present as a marker for method signature decoding.
/// Does not represent an actual method, DO NOT USE FOR THAT.
/// </summary>
internal sealed class MetadataFunctionSignatureSymbol(Symbol? containingSymbol) : FunctionSymbol
{
    public override Symbol? ContainingSymbol => containingSymbol;
    public override ImmutableArray<ParameterSymbol> Parameters => throw new System.NotSupportedException();
    public override TypeSymbol ReturnType => throw new System.NotSupportedException();

    private readonly Dictionary<int, TypeParameterSymbol> typeParameters = [];

    public TypeParameterSymbol GetGenericArgument(int index)
    {
        if (!this.typeParameters.TryGetValue(index, out var typeParam))
        {
            typeParam = new SynthetizedTypeParameterSymbol(this, $"T{index}");
            this.typeParameters.Add(index, typeParam);
        }
        return typeParam;
    }
}
