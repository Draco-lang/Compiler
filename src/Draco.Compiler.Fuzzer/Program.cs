using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Fuzzing;
using Draco.Fuzzing.Tui;
using Draco.Fuzzing.Tui.Addons;
using Terminal.Gui;

namespace Draco.Compiler.Fuzzer;

internal sealed class CompilerFuzzerWindow(IFuzzer fuzzer)
    : FuzzerWindow(fuzzer)
{
    protected override IEnumerable<View> Layout(IReadOnlyDictionary<string, View> views)
    {
        var coverageFrame = (FrameView)views["CoverageScale"];
        coverageFrame.Width = Dim.Fill();

        var currentInputFrame = (FrameView)views["CurrentInput"];
        currentInputFrame.Y = Pos.Bottom(coverageFrame);
        currentInputFrame.Width = Dim.Percent(50);
        currentInputFrame.Height = Dim.Percent(50);

        var minimizedInputFrame = (FrameView)views["MinimizedInput"];
        minimizedInputFrame.Y = Pos.Bottom(coverageFrame);
        minimizedInputFrame.X = Pos.Right(currentInputFrame);
        minimizedInputFrame.Width = Dim.Fill();
        minimizedInputFrame.Height = Dim.Percent(50);

        var inputFrame = (FrameView)views["InputQueue"];
        inputFrame.Y = Pos.Bottom(currentInputFrame);
        inputFrame.Width = Dim.Fill();
        inputFrame.Height = Dim.Fill();

        return [coverageFrame, currentInputFrame, minimizedInputFrame, inputFrame];
    }
}

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
        var window = new CompilerFuzzerWindow(fuzzer);
        window.AddAddon(new StartStopAddon());
        window.AddAddon(new ImportInputAddon<SyntaxTree>
        {
            Extensions = [".draco"],
            Parse = text => SyntaxTree.Parse(text),
        });
        window.AddAddon(new InputQueueAddon<SyntaxTree>()
        {
            MaxVisualizedItems = 5000,
        });
        window.AddAddon(new CoverageScaleAddon());
        window.AddAddon(new CurrentInputAddon<SyntaxTree>());
        window.AddAddon(new MinimizedInputAddon<SyntaxTree>());
        window.Initialize();
        Application.Run(Application.Top);

#if false
        var settings = ParseSettings(args);

        Application.Init();
        Application.MainLoop.Invoke(() =>
        {
            var runMode = GetRunMode(settings);

            var debuggerWindow = new TuiTracer();
            var fuzzer = runMode == RunMode.InProcess
                ? FuzzerFactory.CreateInProcess(debuggerWindow, settings.Seed)
                : FuzzerFactory.CreateOutOfProcess(debuggerWindow, settings.Seed, settings.MaxDegreeOfParallelism);
            debuggerWindow.SetFuzzer(fuzzer);

            // Add any pre-registered files
            var addedTrees = settings.InitialFiles
                .Select(f => SyntaxTree.Parse(File.ReadAllText(f)))
                .ToList();
            fuzzer.EnqueueRange(addedTrees);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                fuzzer.Run(CancellationToken.None);
                Application.Shutdown();
            });
        });
        Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(500), loop =>
        {
            Application.Refresh();
            return true;
        });
        Application.Run(Application.Top);
#endif
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
