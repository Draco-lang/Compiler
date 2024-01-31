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

    private static Task LaunchCommand(FileInfo program)
    {
        Application.Init();
        var debuggerWindow = new DebuggerWindow();
        var currentThread = null as Thread;
        Application.MainLoop.Invoke(async () =>
        {
            var host = DebuggerHost.Create();
            var debugger = host.StartProcess(program.FullName);

            void BreakAt(Thread thread, Method? method, SourceRange? range)
            {
                currentThread = thread;

                var callStack = thread.CallStack;
                var methodNames = callStack
                    .Select(f => f.Method.Name)
                    .ToList();
                debuggerWindow!.SetCallStack(methodNames);

                debuggerWindow.SetLocals(callStack[0].Locals);

                var sourceFilesInModule = method?.Module.SourceFiles;
                if (sourceFilesInModule is not null)
                {
                    debuggerWindow.SetSourceFileList(sourceFilesInModule);
                }

                var sourceFile = method?.SourceFile;
                if (sourceFile is not null)
                {
                    debuggerWindow.SetSourceFile(sourceFile, range);
                }
            }

            debugger.OnEventLog += (_, text) => debuggerWindow.Log(text);
            debugger.OnStandardOut += (_, text) => debuggerWindow.AppendStdout(text);
            debugger.OnStandardError += (_, text) => debuggerWindow.AppendStderr(text);

            debugger.OnBreakpoint += (_, a) => BreakAt(a.Thread, a.Breakpoint.Method, a.Breakpoint.Range);
            debugger.OnStep += (_, a) => BreakAt(a.Thread, a.Method, a.Range);

            debuggerWindow.OnStepInto += (_, _) => currentThread?.StepInto();
            debuggerWindow.OnStepOver += (_, _) => currentThread?.StepOver();
            debuggerWindow.OnStepOut += (_, _) => currentThread?.StepOut();

            await debugger.Terminated;
            Application.Refresh();
        });

        Application.Run(Application.Top);
        Application.Shutdown();

        return Task.CompletedTask;
    }
}
