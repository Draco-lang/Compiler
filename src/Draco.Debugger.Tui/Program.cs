using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Command = System.CommandLine.Command;

namespace Draco.Debugger.Tui;

internal class Program
{
    internal static async Task Main(string[] args) =>
        await ConfigureCommands().InvokeAsync(args);

    private static RootCommand ConfigureCommands()
    {
        var programArgument = new Argument<FileInfo>("program", description: "The .NET program to launch");

        // Launch

        var launchCommand = new Command("launch", "Launches a program for debugging")
        {
            programArgument,
        };
        launchCommand.SetHandler(LaunchCommand, programArgument);

        // Run

        return new RootCommand("TUI for the Draco debugger")
        {
            launchCommand,
        };
    }

    private static async Task LaunchCommand(FileInfo program)
    {
        Application.Init();
        var debuggerWindow = new DebuggerWindow();
        var currentThread = null as Thread;
        Application.MainLoop.Invoke(async () =>
        {
            var host = DebuggerHost.Create(FindDbgShim());
            var debugger = host.StartProcess(program.FullName);

            debugger.StandardInput.WriteLine("John");

            debugger.OnEventLog += (_, text) => debuggerWindow.Log(text);
            debugger.OnStandardOut += (_, text) => debuggerWindow.AppendStdout(text);
            debugger.OnStandardError += (_, text) => debuggerWindow.AppendStderr(text);

            debugger.OnBreakpoint += (_, a) =>
            {
                currentThread = a.Thread;

                var callStack = a.Thread.CallStack
                    .Select(f => f.Method.Name)
                    .ToList();
                debuggerWindow.SetCallStack(callStack);

                var sourceFilesInModule = a.Method?.Module.SourceFiles;
                if (sourceFilesInModule is not null)
                {
                    debuggerWindow.SetSourceFileList(sourceFilesInModule);
                }

                var sourceFile = a.SourceFile;
                if (sourceFile is not null)
                {
                    debuggerWindow.SetSourceFile(sourceFile, a.Range);
                }
            };

            debuggerWindow.OnStepInto += (_, _) => currentThread?.StepInto();
            debuggerWindow.OnStepOver += (_, _) => currentThread?.StepOver();
            debuggerWindow.OnStepOut += (_, _) => currentThread?.StepOut();

            // Application.Run(debuggerWindow);

            await debugger.Terminated;
            Application.Refresh();
        });

        Application.Run(Application.Top);
        Application.Shutdown();
    }

    private static string FindDbgShim()
    {
        var root = "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App";

        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException($"Cannot find dbgshim.dll: '{root}' does not exist");
        }

        foreach (var dir in Directory.EnumerateDirectories(root).Reverse())
        {
            var dbgshim = Directory.EnumerateFiles(dir, "dbgshim.dll").FirstOrDefault();
            if (dbgshim is not null) return dbgshim;
        }

        throw new InvalidOperationException($"Failed to find a runtime containing dbgshim.dll under '{root}'");
    }
}
