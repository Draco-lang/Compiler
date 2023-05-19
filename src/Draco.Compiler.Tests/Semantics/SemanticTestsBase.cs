using System.Text;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RoslynMetadataReference = Microsoft.CodeAnalysis.MetadataReference;

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

    private protected static Binder GetDefiningScope(Compilation compilation, Symbol? symbol)
    {
        Assert.NotNull(symbol);
        var syntax = symbol!.DeclaringSyntax;
        Assert.NotNull(syntax);
        var parent = syntax!.Parent;
        Assert.NotNull(parent);
        return compilation.GetBinder(parent!);
    }

    private protected static MetadataReference CompileCSharpToMetadataRef(string code)
    {
        var sourceText = SourceText.From(code, Encoding.UTF8);
        var tree = SyntaxFactory.ParseSyntaxTree(sourceText);

        var defaultReferences = Basic.Reference.Assemblies.Net70.ReferenceInfos.All
            .Select(r => RoslynMetadataReference.CreateFromStream(new MemoryStream(r.ImageBytes)));

        var compilation = CSharpCompilation.Create(
            assemblyName: "Test.dll",
            syntaxTrees: new[] { tree },
            references: defaultReferences,
            options: new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        Assert.True(emitResult.Success);

        stream.Position = 0;
        return MetadataReference.FromPeStream(stream);
    }

    private protected static void AssertDiagnostic(IEnumerable<Diagnostic> diagnostics, DiagnosticTemplate diagTemplate) =>
        Assert.Contains(diagnostics, d => d.Code == diagTemplate.Code);

    private protected static void AssertNotDiagnostic(IEnumerable<Diagnostic> diagnostics, DiagnosticTemplate diagTemplate) =>
        Assert.DoesNotContain(diagnostics, d => d.Code == diagTemplate.Code);
}
