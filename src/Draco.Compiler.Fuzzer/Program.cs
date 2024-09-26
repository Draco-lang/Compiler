using System;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;

namespace Draco.Compiler.Fuzzer;

internal static class Program
{
    private static void Main(string[] args)
    {
        Application.Init();
        Application.MainLoop.Invoke(async () =>
        {
            var choice = MessageBox.Query("Fuzzer Mode", "Run the fuzzer in-process or out-of-process?", "in-process", "out-of-process");
            if (choice == -1)
            {
                Application.Shutdown();
                return;
            }

            var debuggerWindow = new TuiTracer();
            var fuzzer = choice == 0
                ? FuzzerFactory.CreateInProcess(debuggerWindow)
                : FuzzerFactory.CreateOutOfProcess(debuggerWindow);
            debuggerWindow.Fuzzer = fuzzer;

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
}
