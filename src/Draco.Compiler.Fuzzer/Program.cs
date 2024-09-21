using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;
using Terminal.Gui;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Compiler.Fuzzer;

internal static class Program
{
    private static IEnumerable<MetadataReference> BclReferences => ReferenceInfos.All
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    private static readonly MemoryStream peStream = new();

    private static async Task Main(string[] args)
    {
        Application.Init();
        var debuggerWindow = new TuiTracer();

        var fuzzer = new Fuzzer<SyntaxTree, int>
        {
            InstrumentedAssembly = InstrumentedAssembly.FromWeavedAssembly(typeof(Compilation).Assembly),
            CoverageCompressor = CoverageCompressor.NaiveHash,
            FaultDetector = FaultDetector.FilterIdenticalTraces(FaultDetector.Default(TimeSpan.FromSeconds(5))),
            TargetExecutor = TargetExecutor.Create((SyntaxTree tree) => RunCompilation(tree)),
            InputMinimizer = new SyntaxTreeInputMinimizer(),
            InputMutator = new SyntaxTreeInputMutator(),
            Tracer = debuggerWindow,
        };

        fuzzer.Enqueue(SyntaxTree.Parse("""
            func main() {}
            func foo() {}
            func bar() {}
            func baz() {}
            func qux() {}
            """));

        var fuzzerTask = Task.Run(() => fuzzer.Fuzz(CancellationToken.None));

        Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(500), loop =>
        {
            Application.Refresh();
            return true;
        });

        Application.Run(Application.Top);
        await fuzzerTask;
        Application.Shutdown();
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
