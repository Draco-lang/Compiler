using System;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Terminal.Gui;

namespace Draco.Compiler.Fuzzer;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Application.Init();
        var debuggerWindow = new TuiTracer();

        var fuzzer = FuzzerFactory.CreateOutOfProcess(debuggerWindow);
        debuggerWindow.Fuzzer = fuzzer;

        var fuzzerTask = Task.Run(() => fuzzer.Run(CancellationToken.None));

        Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(500), loop =>
        {
            Application.Refresh();
            return true;
        });

        Application.Run(Application.Top);
        await fuzzerTask;
        Application.Shutdown();
    }
}
