using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Terminal.Gui;

namespace Draco.Compiler.Fuzzer;

internal static class Program
{
    private enum RunMode
    {
        InProcess,
        OutOfProcess,
    }

    private readonly record struct Settings(
        int? Seed,
        RunMode? RunMode,
        bool ForceSingleThreaded,
        int? MaxThreads,
        ImmutableArray<string> InitialFiles);

    private static void Main(string[] args)
    {
        var settings = ParseSettings(args);

        if (settings.MaxThreads is not null)
        {
            ThreadPool.SetMaxThreads(settings.MaxThreads.Value, settings.MaxThreads.Value);
        }

        Application.Init();
        Application.MainLoop.Invoke(async () =>
        {
            var runMode = GetRunMode(settings);

            var debuggerWindow = new TuiTracer();
            var fuzzer = runMode == RunMode.InProcess
                ? FuzzerFactory.CreateInProcess(debuggerWindow, settings.Seed)
                : FuzzerFactory.CreateOutOfProcess(debuggerWindow, settings.Seed, settings.ForceSingleThreaded);
            debuggerWindow.SetFuzzer(fuzzer);

            // Add any pre-registered files
            var addedTrees = settings.InitialFiles
                .Select(f => SyntaxTree.Parse(File.ReadAllText(f)))
                .ToList();
            fuzzer.EnqueueRange(addedTrees);

            var fuzzerTask = Task.Run(() => fuzzer.Run(CancellationToken.None));

            await fuzzerTask;
            Application.Shutdown();
        });
        Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(500), loop =>
        {
            Application.Refresh();
            return true;
        });
        Application.Run(Application.Top);
    }

    private static RunMode GetRunMode(Settings settings)
    {
        if (settings.RunMode is not null) return settings.RunMode.Value;

        var choice = MessageBox.Query("Fuzzer Mode", "Run the fuzzer in-process or out-of-process?", "in-process", "out-of-process");
        if (choice == -1) Application.Shutdown();
        return choice == 0 ? RunMode.InProcess : RunMode.OutOfProcess;
    }

    private static Settings ParseSettings(string[] args)
    {
        var argIndex = 0;
        string? GetNextArg() => argIndex < args.Length ? args[argIndex++] : null;

        var seed = null as int?;
        var runMode = null as RunMode?;
        var forceSingleThreaded = false;
        var maxThreads = null as int?;
        var initialFiles = ImmutableArray.CreateBuilder<string>();

        while (true)
        {
            var arg = GetNextArg();
            if (arg is null) break;

            if (arg == "-h" || arg == "--help")
            {
                Console.WriteLine("""
                    Usage: Draco.Compiler.Fuzzer.exe [options]
                    options:
                        -h, --help: Show this help message
                        -s, --seed: The seed to use for random number generation
                        -ip, --in-process: Run the fuzzer in-process
                        -oop, --out-of-process: Run the fuzzer out-of-process
                        -fst, --force-single-threaded: Force the fuzzer to run single-threaded
                        -mt, --max-threads <threads>: The maximum number of threads to use
                        -ad, --add-directory <directory>: Add an entire directory to the initial files
                        -af, --add-file <file>: Add a file to the initial files
                    """);
                Environment.Exit(0);
            }
            else if (arg == "-s" || arg == "--seed")
            {
                if (seed is not null) throw new ArgumentException("seed already set");
                var seedStr = GetNextArg() ?? throw new ArgumentException("missing seed");
                seed = int.Parse(seedStr);
            }
            else if (arg == "-ip" || arg == "--in-process")
            {
                if (runMode is not null) throw new ArgumentException("run-mode already set");
                runMode = RunMode.InProcess;
            }
            else if (arg == "-oop" || arg == "--out-of-process")
            {
                if (runMode is not null) throw new ArgumentException("run-mode already set");
                runMode = RunMode.OutOfProcess;
            }
            else if (arg == "-fst" || arg == "--force-single-threaded")
            {
                if (forceSingleThreaded) throw new ArgumentException("forcing single-threaded already set");
                forceSingleThreaded = true;
            }
            else if (arg == "-mt" ||arg == "--max-threads")
            {
                if (maxThreads is not null) throw new ArgumentException("max-threads already set");
                var maxThreadsStr = GetNextArg() ?? throw new ArgumentException("missing max-threads");
                maxThreads = int.Parse(maxThreadsStr);
            }
            else if (arg == "-ad" || arg == "--add-directory")
            {
                var directory = GetNextArg() ?? throw new ArgumentException("missing directory");
                initialFiles.AddRange(System.IO.Directory.GetFiles(directory));
            }
            else if (arg == "-af" || arg == "--add-file")
            {
                var file = GetNextArg() ?? throw new ArgumentException("missing file");
                initialFiles.Add(file);
            }
            else
            {
                throw new ArgumentException($"unknown argument: {arg}");
            }
        }

        return new Settings(
            Seed: seed,
            RunMode: runMode,
            ForceSingleThreaded: forceSingleThreaded,
            MaxThreads: maxThreads,
            InitialFiles: initialFiles.ToImmutable());
    }
}
