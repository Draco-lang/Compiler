using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Tests.Semantics;

public abstract class SemanticTestsBase
{
    private protected static TSymbol GetInternalSymbol<TSymbol>(ISymbol? symbol)
        where TSymbol : Symbol
    {
        Assert.NotNull(symbol);
        // TODO
        //var symbolBase = (SymbolBase)symbol!;
        //return (TSymbol)symbolBase.Symbol;
        throw new NotImplementedException();
    }

    private protected static void AssertDiagnostic(IEnumerable<Diagnostic> diagnostics, DiagnosticTemplate diagTemplate) =>
        Assert.Contains(diagnostics, d => d.Code == diagTemplate.Code);

    private protected static void AssertNotDiagnostic(IEnumerable<Diagnostic> diagnostics, DiagnosticTemplate diagTemplate) =>
        Assert.DoesNotContain(diagnostics, d => d.Code == diagTemplate.Code);
}
