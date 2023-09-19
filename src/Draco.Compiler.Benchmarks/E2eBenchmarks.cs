using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Benchmarks;

public class E2eBenchmarks : FolderBenchmarkBase
{
    public E2eBenchmarks()
        : base("e2e")
    {
    }

    [Benchmark]
    public void Compile()
    {
        var syntaxTree = SyntaxTree.Parse(this.Input.Code, Path.GetFullPath(this.Input.Path));
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        var peStream = new MemoryStream();
        _ = compilation.Emit(peStream: peStream);
    }
}
