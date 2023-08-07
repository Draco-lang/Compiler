using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Tests.Semantics;

public abstract class SemanticTestsBase
{
    private protected static Compilation CreateCompilation(params SyntaxTree[] syntaxTrees) => Compilation.Create(
        syntaxTrees: syntaxTrees.ToImmutableArray(),
        metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
            .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
            .ToImmutableArray());

    private protected static TSymbol GetInternalSymbol<TSymbol>(ISymbol? symbol)
        where TSymbol : Symbol
    {
        Assert.NotNull(symbol);
        var symbolBase = (SymbolBase)symbol!;
        return (TSymbol)symbolBase.Symbol;
    }

    private protected static TMember GetMemberSymbol<TMember>(Symbol parent, string memberName) where TMember : Symbol =>
        parent.Members.OfType<TMember>().Single(x => x.Name == memberName);

    private protected static Symbol GetMetadataSymbol(Compilation compilation, string? assemblyName, params string[] path)
    {
        assemblyName ??= TestUtilities.DefaultAssemblyName;
        var asm = compilation.MetadataAssemblies.Values.Single(a => a.AssemblyName.Name == assemblyName);
        return asm.RootNamespace.Lookup(path.ToImmutableArray()).First();
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
