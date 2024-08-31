using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RoslynMetadataReference = Microsoft.CodeAnalysis.MetadataReference;
using RoslynSyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSharpSourceText = Microsoft.CodeAnalysis.Text.SourceText;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Api.Diagnostics;
using System.Reflection;
using Binder = Draco.Compiler.Internal.Binding.Binder;
using Draco.Compiler.Internal;
using System.Runtime.CompilerServices;

namespace Draco.Compiler.Tests;

internal static class TestUtilities
{
    public const string DefaultAssemblyName = "Test.dll";

    private static IEnumerable<Basic.Reference.Assemblies.Net80.ReferenceInfo> Net8Bcl =>
        Basic.Reference.Assemblies.Net80.ReferenceInfos.All;

    public static ImmutableArray<MetadataReference> BclReferences { get; } = Net8Bcl
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
        .ToImmutableArray();

    public static string ToPath(params string[] parts) => Path.GetFullPath(Path.Combine(parts));

    public static void AssertDiagnostics(
        IEnumerable<Diagnostic> gotDiagnostics,
        params DiagnosticTemplate[] expectedDiagnostics) =>
        AssertDiagnostics(gotDiagnostics, expectedDiagnostics.AsEnumerable());

    public static void AssertDiagnostics(
        IEnumerable<Diagnostic> gotDiagnostics,
        IEnumerable<DiagnosticTemplate> expectedDiagnostics)
    {
        var gotDiagnosticTemplates = gotDiagnostics
            .Select(d => d.Template)
            .ToList();
        foreach (var expectedDiag in expectedDiagnostics)
        {
            Assert.Contains(expectedDiag, gotDiagnosticTemplates);
        }
    }

    public static void AssertNotDiagnostics(
        IEnumerable<Diagnostic> diagnostics,
        params DiagnosticTemplate[] expectedMissingDiagnostics) =>
        AssertNotDiagnostics(diagnostics, expectedMissingDiagnostics.AsEnumerable());

    public static void AssertNotDiagnostics(
        IEnumerable<Diagnostic> diagnostics,
        IEnumerable<DiagnosticTemplate> expectedMissingDiagnostics)
    {
        var gotDiagnosticTemplates = diagnostics
            .Select(d => d.Template)
            .ToList();
        foreach (var expectedDiag in expectedMissingDiagnostics)
        {
            Assert.DoesNotContain(expectedDiag, gotDiagnosticTemplates);
        }
    }

    #region Semantic utilities

    public static TSymbol GetInternalSymbol<TSymbol>(ISymbol? symbol)
        where TSymbol : Symbol
    {
        Assert.NotNull(symbol);
        Assert.IsType<TSymbol>(symbol);
        var symbolBase = (SymbolBase)symbol!;
        return (TSymbol)symbolBase.Symbol;
    }

    public static TMember GetMember<TMember>(Symbol parent, string memberName)
        where TMember : Symbol
    {
        var member = parent.Members.OfType<TMember>().Where(x => x.Name == memberName).FirstOrDefault();
        if (member is null)
        {
            Assert.Fail($"member '{memberName}' not found in '{parent.Name}'");
        }
        return member;
    }

    public static Binder GetDefiningScope(Symbol? symbol)
    {
        Assert.NotNull(symbol);
        Assert.NotNull(symbol.DeclaringCompilation);

        var syntax = symbol.DeclaringSyntax;
        Assert.NotNull(syntax);

        var parent = syntax.Parent;
        Assert.NotNull(parent);

        return symbol.DeclaringCompilation.GetBinder(parent!);
    }

    #endregion

    #region Draco compilation

    public static TResult Invoke<TResult>(
        Assembly assembly,
        string methodName = CompilerConstants.EntryPointName,
        IEnumerable<object?>? args = null,
        TextReader? stdin = null,
        TextWriter? stdout = null,
        string moduleName = CompilerConstants.DefaultModuleName)
    {
        Console.SetIn(stdin ?? Console.In);
        Console.SetOut(stdout ?? Console.Out);

        // NOTE: nested typed are not separated by . but by + in IL, thats the reason for the replace
        var method = assembly
            .GetType(moduleName.Replace('.', '+'))?
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (TResult?)method?.Invoke(null, args?.ToArray());
        return result!;
    }

    public static Assembly CompileToAssembly(
        string sourceCode,
        IEnumerable<MetadataReference>? additionalReferences = null) => CompileToAssembly(CreateCompilation(
            sourceCode: sourceCode,
            additionalReferences: additionalReferences));

    public static Assembly CompileToAssembly(Compilation compilation)
    {
        // TODO: We need a custom load context
        // And also have C# compilations magically resolve here
        // Maybe we can use the test name to have keys of loaded stuff
        // A CWT might be the best here
        throw new NotImplementedException();
    }

    public static MemoryStream CompileToMemory(Compilation compilation)
    {
        var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream: peStream);
        peStream.Position = 0;
        return peStream;
    }

    public static Compilation CreateCompilation(
        string sourceCode,
        IEnumerable<MetadataReference>? additionalReferences = null) => CreateCompilation(
            syntaxTree: SyntaxTree.Parse(sourceCode),
            additionalReferences: additionalReferences);

    public static Compilation CreateCompilation(
        SyntaxTree syntaxTree,
        IEnumerable<MetadataReference>? additionalReferences = null) => CreateCompilation(
            syntaxTrees: [syntaxTree],
            additionalReferences: additionalReferences);

    public static Compilation CreateCompilation(
        IEnumerable<SyntaxTree> syntaxTrees,
        IEnumerable<MetadataReference>? additionalReferences = null) => Compilation.Create(
            syntaxTrees: syntaxTrees.ToImmutableArray(),
            metadataReferences: [.. BclReferences, .. additionalReferences]);

    #endregion

    #region C# compilation

    public static MetadataReference CompileCSharpToMetadataReference(
        string code,
        string assemblyName = DefaultAssemblyName,
        IEnumerable<Stream>? aditionalReferences = null,
        Stream? xmlDocStream = null)
    {
        var stream = CompileCSharpToStream(code, assemblyName, aditionalReferences, xmlDocStream);
        return MetadataReference.FromPeStream(stream, xmlDocStream);
    }

    public static Stream CompileCSharpToStream(
        string code,
        string assemblyName = DefaultAssemblyName,
        IEnumerable<Stream>? aditionalReferences = null,
        Stream? xmlDocStream = null)
    {
        aditionalReferences ??= [];
        var sourceText = CSharpSourceText.From(code, Encoding.UTF8);
        var tree = RoslynSyntaxFactory.ParseSyntaxTree(sourceText);

        var defaultReferences = Net8Bcl
            .Select(r => RoslynMetadataReference.CreateFromStream(new MemoryStream(r.ImageBytes)))
            .Concat(aditionalReferences.Select(r => RoslynMetadataReference.CreateFromStream(r)));

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: [tree],
            references: defaultReferences,
            options: new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream, xmlDocumentationStream: xmlDocStream);
        Assert.True(emitResult.Success);

        stream.Position = 0;

        if (xmlDocStream is not null) xmlDocStream.Position = 0;
        return stream;
    }

    #endregion
}
