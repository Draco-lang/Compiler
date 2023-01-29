using Draco.Compiler.Api.Diagnostics;
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

    private protected static void AssertDiagnostic(IEnumerable<Diagnostic> diagnostics, DiagnosticTemplate diagTemplate) =>
        Assert.Contains(diagnostics, d => d.Code == diagTemplate.Code);

    private protected static void AssertNotDiagnostic(IEnumerable<Diagnostic> diagnostics, DiagnosticTemplate diagTemplate) =>
        Assert.DoesNotContain(diagnostics, d => d.Code == diagTemplate.Code);
}
