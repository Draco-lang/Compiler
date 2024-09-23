using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Compiler.Fuzzer;

internal static class FuzzerFactory
{
    private static ICoverageCompressor<int> CoverageCompressor => Fuzzing.CoverageCompressor.SimdHash;
    private static IInputMinimizer<SyntaxTree> InputMinimizer => new SyntaxTreeInputMinimizer();
    private static IInputMutator<SyntaxTree> InputMutator => new SyntaxTreeInputMutator();
    private static TimeSpan Timeout => TimeSpan.FromSeconds(5);

    public static Fuzzer<SyntaxTree, int> CreateInProcess(ITracer<SyntaxTree> tracer)
    {
        // Things we share between compilations
        var bclReferences = ReferenceInfos.All
            .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
            .ToImmutableArray();
        var peStream = new MemoryStream();
        Compilation? previousCompilation = null;

        void RunCompilation(SyntaxTree syntaxTree)
        {
            // Cache compilation to optimize discovered metadata references
            if (previousCompilation is null)
            {
                previousCompilation = Compilation.Create(
                    syntaxTrees: [syntaxTree],
                    metadataReferences: bclReferences);
            }
            else
            {
                previousCompilation = previousCompilation
                    .UpdateSyntaxTree(previousCompilation.SyntaxTrees[0], syntaxTree);
            }
            // NOTE: We reuse the same memory stream to de-stress memory usage a little
            peStream.Position = 0;
            previousCompilation.Emit(peStream: peStream);
        }

        var instrumentedAssembly = InstrumentedAssembly.FromWeavedAssembly(typeof(Compilation).Assembly);
        return new()
        {
            CoverageCompressor = CoverageCompressor,
            CoverageReader = CoverageReader.FromInstrumentedAssembly(instrumentedAssembly),
            FaultDetector = FaultDetector.FilterIdenticalTraces(FaultDetector.DefaultInProcess(Timeout)),
            TargetExecutor = TargetExecutor.Assembly(instrumentedAssembly, (SyntaxTree tree) => RunCompilation(tree)),
            InputMinimizer = InputMinimizer,
            InputMutator = InputMutator,
            Tracer = tracer,
        };
    }

    public static Fuzzer<SyntaxTree, int> CreateOutOfProcess(ITracer<SyntaxTree> tracer)
    {
        // TODO
        throw new NotImplementedException();
    }
}
