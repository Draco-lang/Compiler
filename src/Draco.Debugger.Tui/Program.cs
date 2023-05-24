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
        Application.MainLoop.Invoke(async () =>
        {
            var host = DebuggerHost.Create(FindDbgShim());
            var debugger = await host.StartProcess(program.FullName);

#if false
            debugger.OnBreakpoint += async (_, args) =>
            {
                debuggerWindow.SourceText.Text = args.SourceFile?.Text ?? string.Empty;
                if (args.Range is not null)
                {
                    var seq = args.Range.Value;
                    debuggerWindow.SourceText.SelectionStartRow = seq.StartLine;
                    debuggerWindow.SourceText.SelectionStartColumn = seq.StartColumn;
                    debuggerWindow.SourceText.CursorPosition = new(x: seq.EndColumn - 1, y: seq.EndLine - 1);
                    debuggerWindow.SourceText.Selecting = true;
                }
                Application.Refresh();
                await Task.Delay(3000);
                debugger.Resume();
            };

            var mainFile = debugger.SourceFiles.Keys.First();

            debugger.SetBreakpoint(mainFile, lineNumber: 3);
            debugger.SetBreakpoint(mainFile, lineNumber: 4);
            debugger.SetBreakpoint(mainFile, lineNumber: 5);
            debugger.SetBreakpoint(mainFile, lineNumber: 6);
            debugger.Resume();
#endif

            // Application.Run(debuggerWindow);

            await debugger.Terminated;
        });

        Application.Run(debuggerWindow);
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
