using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Compiler.Fuzzer;

internal static class Program
{
    private static IEnumerable<MetadataReference> BclReferences => ReferenceInfos.All
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    private static readonly MemoryStream peStream = new();

    private static void Main(string[] args)
    {
        var fuzzer = new Fuzzer<SyntaxTree, int>
        {
            InstrumentedAssembly = InstrumentedAssembly.FromWeavedAssembly(typeof(Compilation).Assembly),
            CoverageCompressor = CoverageCompressor.Hash,
            FaultDetector = FaultDetector.Default(TimeSpan.FromSeconds(5)),
            TargetExecutor = TargetExecutor.Create((SyntaxTree tree) => RunCompilation(tree)),
            InputMinimizer = new SyntaxTreeInputMinimizer(),
            InputMutator = new SyntaxTreeInputMutator(),
            Tracer = new ConsoleTracer(),
        };

        fuzzer.Enqueue(SyntaxTree.Parse("""
            func main() {}
            """));

        fuzzer.Fuzz(CancellationToken.None);
    }

    private static void RunCompilation(SyntaxTree syntaxTree)
    {
        // NOTE: We reuse the same memory stream to de-stress memory usage a little
        peStream.Position = 0;
        var compilation = Compilation.Create(
            syntaxTrees: [syntaxTree],
            metadataReferences: BclReferences.ToImmutableArray());
        compilation.Emit(peStream: peStream);
    }
}
