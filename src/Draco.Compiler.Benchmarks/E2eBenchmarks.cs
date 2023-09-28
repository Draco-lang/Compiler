using System.Collections.Immutable;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Benchmarks;

public class E2eBenchmarks : FolderBenchmarkBase
{
    private MemoryStream peStream = null!;

    public E2eBenchmarks()
        : base("e2e")
    {
    }

    [IterationSetup]
    public void Setup()
    {
        this.peStream = new();
    }

    [Benchmark]
    public EmitResult CompileSingleThreaded()
    {
        var syntaxTree = SyntaxTree.Parse(this.Input.Code, Path.GetFullPath(this.Input.Path));
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            useMultithreading: false);
        return compilation.Emit(peStream: this.peStream);
    }

    [Benchmark]
    public EmitResult CompileMultiThreaded()
    {
        var syntaxTree = SyntaxTree.Parse(this.Input.Code, Path.GetFullPath(this.Input.Path));
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            useMultithreading: true);
        return compilation.Emit(peStream: this.peStream);
    }
}
