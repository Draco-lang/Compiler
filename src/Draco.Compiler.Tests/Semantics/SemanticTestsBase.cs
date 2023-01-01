using Draco.Compiler.Api.Semantics;
using IInternalSymbol = Draco.Compiler.Internal.Semantics.Symbols.ISymbol;

namespace Draco.Compiler.Tests.Semantics;

public abstract class SemanticTestsBase
{
    private protected static TSymbol GetInternalSymbol<TSymbol>(ISymbol? symbol)
        where TSymbol : IInternalSymbol
    {
        Assert.NotNull(symbol);
        var symbolBase = (SymbolBase)symbol!;
        return (TSymbol)symbolBase.Symbol;
    }
}
