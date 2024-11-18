using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A compiler-generated this parameter.
/// </summary>
internal sealed class SynthetizedThisParameterSymbol(FunctionSymbol containingSymbol) : ParameterSymbol
{
    public override FunctionSymbol ContainingSymbol => containingSymbol;
    public override bool IsThis => true;

    public override TypeSymbol Type => LazyInitializer.EnsureInitialized(ref this.type, this.BuildType);
    private TypeSymbol? type;

    private TypeSymbol BuildType()
    {
        var containingType = this.ContainingSymbol.AncestorChain
            .OfType<TypeSymbol>()
            .First();

        if (!containingType.IsGenericDefinition) return containingType;

        var genericArgs = containingType.GenericParameters.Cast<TypeSymbol>().ToImmutableArray();
        return containingType.GenericInstantiate(containingType.ContainingSymbol, genericArgs);
    }
}
