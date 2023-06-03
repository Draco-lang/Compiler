using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Tests.Semantics;

public abstract class SemanticTestsBase
{
    private protected static TSymbol GetInternalSymbol<TSymbol>(ISymbol? symbol)
        where TSymbol : Symbol
    {
        Assert.NotNull(symbol);
        var symbolBase = (SymbolBase)symbol!;
        return (TSymbol)symbolBase.Symbol;
    }

    private protected static TMember GetMemberSymbol<TMember>(Symbol parent, string memberName) where TMember : Symbol =>
        (TMember)parent.Members.Single(x => x.Name == memberName);

    private protected static Symbol GetMetadataSymbol(Compilation compilation, string? @namespace, params string[] path)
    {
        @namespace = @namespace ?? string.Empty;
        var asm = compilation.MetadataAssemblies.Values.Single(a => a.RootNamespace == @namespace);
        return asm.RootNamespace.Lookup(path.ToImmutableArray()).Single();

        Symbol? Recurse(Symbol parent, string[] path)
        {
            if (path.Length == 0) return parent;
            var sym = parent.Members.Where(x => x.Name == path[0]);
            if (sym.Any()) return Recurse(sym.First(), path[1..]);
            return null;
        }
    }

    private protected static Binder GetDefiningScope(Compilation compilation, Symbol? symbol)
    {
        Assert.NotNull(symbol);
        var syntax = symbol!.DeclaringSyntax;
        Assert.NotNull(syntax);
        var parent = syntax!.Parent;
        Assert.NotNull(parent);
        return compilation.GetBinder(parent!);
    }

    private protected static void AssertDiagnostic(IEnumerable<Diagnostic> diagnostics, DiagnosticTemplate diagTemplate) =>
        Assert.Contains(diagnostics, d => d.Code == diagTemplate.Code);

    private protected static void AssertNotDiagnostic(IEnumerable<Diagnostic> diagnostics, DiagnosticTemplate diagTemplate) =>
        Assert.DoesNotContain(diagnostics, d => d.Code == diagTemplate.Code);
}
