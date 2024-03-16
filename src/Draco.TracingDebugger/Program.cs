using Draco.Debugger;

namespace Draco.TracingDebugger;

internal class Program
{
    static async Task Main(string[] args)
    {
        var debuggerHost = DebuggerHost.Create();
        var path = @"C:\dev\ConsoleApp36\ConsoleApp36\bin\Debug\net8.0\ConsoleApp36.exe";
        var debugger = debuggerHost.StartProcess(path);
        await debugger.Ready;
        while (!debugger.Terminated.IsCompleted)
        {
            var lastFrame = debugger.MainThread.CallStack[^1];
            var lines = lastFrame.Method.SourceFile?.Lines.AsEnumerable();
            if (lines is null || !lastFrame.Range.HasValue)
            {
                Console.WriteLine("???");
                continue;
            }
            var filtered = lines.Skip(lastFrame.Range.Value.Start.Line).Take(lastFrame.Range.Value.End.Line - lastFrame.Range.Value.Start.Line + 1);

            Console.WriteLine(string.Join("\n", filtered.Select(s => s.ToString()) ?? []));
            debugger.MainThread.StepOver();
            System.Threading.Thread.Sleep(5000);
        }

    }
}
