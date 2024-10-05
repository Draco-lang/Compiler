using System.Collections.Immutable;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Benchmarks;

public class E2eBenchmarks() : FolderBenchmarkBase("inputs")
{
    // 64 KB should be enough for anyone, right Bill?
    private readonly MemoryStream peStream = new(1024 * 64);

    [IterationSetup]
    public void Setup() => this.peStream.Position = 0;

    [Benchmark]
    public EmitResult Compile()
    {
        var syntaxTree = SyntaxTree.Parse(this.Input.Code, Path.GetFullPath(this.Input.Path));
        var compilation = Compilation.Create(
            syntaxTrees: [syntaxTree],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        return compilation.Emit(peStream: this.peStream);
    }
}
