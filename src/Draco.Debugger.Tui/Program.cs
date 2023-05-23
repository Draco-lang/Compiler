using System.CommandLine;
using Terminal.Gui;
using Command = System.CommandLine.Command;

namespace Draco.Debugger.Tui;

internal class Program
{
    internal static void Main(string[] args) =>
        ConfigureCommands().Invoke(args);

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
        var host = DebuggerHost.Create(FindDbgShim());
        var debugger = await host.StartProcess(program.FullName);

        var mainFile = debugger.SourceFiles.Keys.First();

        debugger.SetBreakpoint(mainFile, lineNumber: 4);
        debugger.Resume();

        await Task.Delay(5000);

        debugger.Resume();

        await debugger.Terminated;
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
