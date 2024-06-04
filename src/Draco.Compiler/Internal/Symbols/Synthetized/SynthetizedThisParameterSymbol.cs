using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Represents the "this" parameter of a function generated by the compiler.
/// </summary>
internal sealed class SynthetizedThisParameterSymbol : ParameterSymbol
{
    public override FunctionSymbol ContainingSymbol { get; }
    public override string Name => "this";
    public override bool IsThis => true;

    public override TypeSymbol Type => LazyInitializer.EnsureInitialized(ref this.type, this.BuildType);
    private TypeSymbol? type;

    public SynthetizedThisParameterSymbol(FunctionSymbol containingSymbol)
    {
        this.ContainingSymbol = containingSymbol;
    }

    private TypeSymbol BuildType()
    {
        var thisType = (TypeSymbol)this.ContainingSymbol.ContainingSymbol!;
        return thisType.IsValueType
            ? new ReferenceTypeSymbol(thisType)
            : thisType;
    }
}