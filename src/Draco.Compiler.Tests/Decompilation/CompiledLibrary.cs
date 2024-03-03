using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Tests.Utilities;

namespace Draco.Compiler.Tests.Decompilation;

internal sealed class CompiledLibrary
{
    public CompiledLibrary(Compilation compilation, MetadataCodegen codeGen, ReadOnlyMemory<byte> assemblyBytes)
    {
        Compilation = compilation;
        Codegen = codeGen;
        AssemblyBytes = assemblyBytes;
    }

    public Compilation Compilation { get; }
    public MetadataCodegen Codegen { get; }
    public ReadOnlyMemory<byte> AssemblyBytes { get; }

    private PEReader GetPeReader() => _reader ??= new PEReader(new ReadOnlyMemoryStream(AssemblyBytes));
    private PEReader? _reader;

    public Assembly GetLoadedAssembly() => _loadedAssembly ??= LoadAssembly();
    private Assembly? _loadedAssembly;

    private Assembly LoadAssembly()
    {
        Debug.Assert(_loadedAssembly is null);

        var loadContext = new AssemblyLoadContext("testLoadContext");

        var asm = loadContext.LoadFromStream(new ReadOnlyMemoryStream(AssemblyBytes));
        return asm;
    }

    public void AssertIL(string memberName, string il)
    {
        var peReader = GetPeReader();
        var compiledAssemblyReference = MetadataReference.FromPeReader(peReader);
        var c = Compilation.Create(
                    ImmutableArray<SyntaxTree>.Empty,
                    Compilation.MetadataReferences.Add(compiledAssemblyReference));


        // TODO: very clunky lookup which won't work if multiple method are defined
        var member =
            Compilation
                .GetSemanticModel(Compilation.SyntaxTrees[0])
                .GetAllDefinedSymbols(Compilation.SyntaxTrees[0].Root)
                .OfType<SymbolBase>()
                .Single(s => s.Symbol is Draco.Compiler.Internal.Symbols.FunctionSymbol);

        var func = Assert.IsAssignableFrom<MetadataMethodSymbol>(member);

        var actualIl = CilFormatter.VisualizeIl(this, func, peReader, compiledAssemblyReference.MetadataReader);

        Assert.True(CilSpaceAgnosticStringComparer.Ordinal.Equals(il, actualIl));
    }

    public TypeInfo GetTypeInfo(string type)
    {
        var asm = GetLoadedAssembly();

        var typeInfo = asm.GetType(type);

        Assert.NotNull(typeInfo);

        return typeInfo.GetTypeInfo();
    }

    public MethodInfo GetMethodInfo(string member)
    {
        var memberPath = member.Split('.');

        var type = GetTypeInfo(string.Join(".", memberPath.SkipLast(1)));

        var method = type.GetMethod(memberPath[^1]);

        Assert.NotNull(method);

        return method;
    }

    public void Execute(string member, Action<MethodExecutionBuilder> configureBuilder)
    {
        var method = GetMethodInfo(member);

        var builder = new MethodExecutionBuilder();

        configureBuilder.Invoke(builder);

        Debug.Assert(builder.CheckStdOutActon is { } || builder.CheckReturnAction is { });

        IDisposable disposable = Disposable.Empty;

        string? stdOut = null;

        if (builder.CheckStdOutActon is { })
        {
            var writer = new StringWriter();
            var defaultStdOut = Console.Out;

            Console.SetOut(writer);

            disposable = Disposable.Create(() =>
            {
                Console.SetOut(defaultStdOut);
                stdOut = writer.ToString();
            });
        }

        object? result;

        using (disposable)
        {
            result = method.Invoke(builder.Instance, builder.Arguments);
        }

        builder.CheckReturnAction?.Invoke(result);
        builder.CheckStdOutActon?.Invoke(stdOut!);
    }
}
