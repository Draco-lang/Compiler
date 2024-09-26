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

    private readonly record struct Settings(RunMode? RunMode, ImmutableArray<string> InitialFiles);

    private static void Main(string[] args)
    {
        var settings = ParseSettings(args);

        Application.Init();
        Application.MainLoop.Invoke(async () =>
        {
            var runMode = GetRunMode(settings);

            var debuggerWindow = new TuiTracer();
            var fuzzer = runMode == RunMode.InProcess
                ? FuzzerFactory.CreateInProcess(debuggerWindow)
                : FuzzerFactory.CreateOutOfProcess(debuggerWindow);
            debuggerWindow.Fuzzer = fuzzer;

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

        var runMode = null as RunMode?;
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
                        -ip, --in-process: Run the fuzzer in-process
                        -op, --out-of-process: Run the fuzzer out-of-process
                        -ad, --add-directory <directory>: Add an entire directory to the initial files
                        -af, --add-file <file>: Add a file to the initial files
                    """);
                Environment.Exit(0);
            }
            if (arg == "-ip" || arg == "--in-process")
            {
                if (runMode is not null) throw new ArgumentException("run-mode already set");
                runMode = RunMode.InProcess;
            }
            if (arg == "-op" || arg == "--out-of-process")
            {
                if (runMode is not null) throw new ArgumentException("run-mode already set");
                runMode = RunMode.OutOfProcess;
            }
            if (arg == "-ad" || arg == "--add-directory")
            {
                var directory = GetNextArg() ?? throw new ArgumentException("missing directory");
                initialFiles.AddRange(System.IO.Directory.GetFiles(directory));
            }
            if (arg == "-af" || arg == "--add-file")
            {
                var file = GetNextArg() ?? throw new ArgumentException("missing file");
                initialFiles.Add(file);
            }
        }

        return new Settings(runMode, initialFiles.ToImmutable());
    }
}
