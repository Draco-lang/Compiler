using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;
using Draco.Fuzzing.Components;
using Draco.Fuzzing.Tracing;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Compiler.Fuzzer;

internal static class FuzzerFactory
{
    private static ICoverageReader CoverageReader => Fuzzing.Components.CoverageReader.Default;
    private static ICoverageCompressor<int> CoverageCompressor => Fuzzing.Components.CoverageCompressor.SimdHash;
    private static IInputMinimizer<SyntaxTree> InputMinimizer => new SyntaxTreeInputMinimizer();
    private static IInputMutator<SyntaxTree> InputMutator => new SyntaxTreeInputMutator();
    private static IInputCompressor<SyntaxTree, string> InputCompressor => Fuzzing.Components.InputCompressor.String(text => SyntaxTree.Parse(text));
    private static TimeSpan Timeout => TimeSpan.FromSeconds(5);

    public static Fuzzer<SyntaxTree, string, int> CreateInProcess(ITracer<SyntaxTree> tracer, int? seed)
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
        var settings = seed is null
            ? FuzzerSettings.DefaultInProcess with { CompressAfterQueueSize = 5000 }
            : FuzzerSettings.DefaultInProcess with { CompressAfterQueueSize = 5000, Seed = seed.Value };
        return new(settings)
        {
            CoverageCompressor = CoverageCompressor,
            CoverageReader = CoverageReader,
            TargetExecutor = TargetExecutor.InProcess(instrumentedAssembly, (SyntaxTree tree) => RunCompilation(tree)),
            FaultDetector = FaultDetector.FilterIdenticalTraces(FaultDetector.InProcess(Timeout)),
            InputMinimizer = InputMinimizer,
            InputMutator = InputMutator,
            InputCompressor = InputCompressor,
            Tracer = new LockSyncTracer<SyntaxTree>(tracer, new()),
        };
    }

    public static Fuzzer<SyntaxTree, string, int> CreateOutOfProcess(ITracer<SyntaxTree> tracer, int? seed, int? maxParallelism)
    {
        static ProcessStartInfo CreateStartInfo(SyntaxTree syntaxTree)
        {
            // dotnet exec Draco.Compiler.DevHost -- compile-base64 <base64-encoded-source-code>
            var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(syntaxTree.ToString());
            var base64Source = Convert.ToBase64String(utf8Bytes);
            return new()
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "exec",
                    "Draco.Compiler.DevHost.dll",
                    "compile-base64",
                    base64Source,
                },
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }

        var instrumentedAssembly = InstrumentedAssembly.FromWeavedAssembly(typeof(Compilation).Assembly);
        var settings = seed is null
            ? FuzzerSettings.DefaultOutOfProcess with { CompressAfterQueueSize = 5000, }
            : FuzzerSettings.DefaultOutOfProcess with { CompressAfterQueueSize = 5000, Seed = seed.Value, MaxDegreeOfParallelism = maxParallelism ?? -1 };
        return new(settings)
        {
            CoverageReader = CoverageReader,
            CoverageCompressor = CoverageCompressor,
            TargetExecutor = TargetExecutor.OutOfProcess<SyntaxTree>(instrumentedAssembly, CreateStartInfo),
            FaultDetector = FaultDetector.OutOfProcess(Timeout),
            InputMinimizer = InputMinimizer,
            InputMutator = InputMutator,
            InputCompressor = InputCompressor,
            Tracer = new LockSyncTracer<SyntaxTree>(tracer, new()),
        };
    }
}
