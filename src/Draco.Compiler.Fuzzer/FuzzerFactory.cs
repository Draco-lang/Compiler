using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Compiler.Fuzzer;

internal static class FuzzerFactory
{
    private static ICoverageReader CoverageReader => Fuzzing.CoverageReader.Default;
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
            CoverageReader = CoverageReader,
            TargetExecutor = TargetExecutor.InProcess(instrumentedAssembly, (SyntaxTree tree) => RunCompilation(tree)),
            FaultDetector = FaultDetector.FilterIdenticalTraces(FaultDetector.InProcess(Timeout)),
            InputMinimizer = InputMinimizer,
            InputMutator = InputMutator,
            Tracer = tracer,
        };
    }

    public static Fuzzer<SyntaxTree, int> CreateOutOfProcess(ITracer<SyntaxTree> tracer)
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
        return new()
        {
            CoverageReader = CoverageReader,
            CoverageCompressor = CoverageCompressor,
            TargetExecutor = TargetExecutor.OutOfProcess<SyntaxTree>(instrumentedAssembly, CreateStartInfo),
            FaultDetector = FaultDetector.OutOfProcess(Timeout),
            InputMinimizer = InputMinimizer,
            InputMutator = InputMutator,
            Tracer = tracer,
        };
    }
}
