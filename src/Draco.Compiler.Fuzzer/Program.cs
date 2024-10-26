using System;
using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;
using Draco.Fuzzing.Tui.Addons;
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
        int? MaxDegreeOfParallelism,
        ImmutableArray<string> InitialFiles);

    private static void Main(string[] args)
    {
        var settings = ParseSettings(args);
        var runMode = GetRunMode(settings);
        var fuzzer = runMode == RunMode.InProcess
            ? FuzzerFactory.CreateInProcess(settings.Seed)
            : FuzzerFactory.CreateOutOfProcess(settings.Seed, settings.MaxDegreeOfParallelism);

        Application.Init();
        var application = CompilerFuzzerWindow.Create(fuzzer);
        Application.MainLoop.Invoke(() =>
        {
            var runMode = GetRunMode(settings);

            // Add any pre-registered files through the addon
            var importInputAddon = application.RequireAddon<ImportInputAddon<SyntaxTree>>("ImportInput", "main application");
            importInputAddon.ImportFiles(settings.InitialFiles);

            // Run the fuzzer through the addon
            var startStopAddon = application.RequireAddon<StartStopAddon>("StartStop", "main application");
            startStopAddon.StartFuzzer();
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
        var maxDegreeOfParallelism = null as int?;
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
                        -mp, --max-parallelism <degree>: The maximum degree of parallelism to use
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
            else if (arg == "-mp" || arg == "--max-parallelism")
            {
                if (maxDegreeOfParallelism is not null) throw new ArgumentException("max-parallelism already set");
                var degreeStr = GetNextArg() ?? throw new ArgumentException("missing degree");
                maxDegreeOfParallelism = int.Parse(degreeStr);
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
            MaxDegreeOfParallelism: maxDegreeOfParallelism,
            InitialFiles: initialFiles.ToImmutable());
    }
}
