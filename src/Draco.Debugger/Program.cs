using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var host = DebuggerHost.Create(FindDbgShim());
        var debugger = await host.StartProcess("c:/TMP/DracoTest/bin/Debug/net7.0/DracoTest.exe");

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
            throw new InvalidOperationException($"Cannot find dbgshim.dll: '{root}' does not exist");

        foreach (var dir in Directory.EnumerateDirectories(root).Reverse())
        {
            var dbgshim = Directory.EnumerateFiles(dir, "dbgshim.dll").FirstOrDefault();

            if (dbgshim != null)
                return dbgshim;
        }

        throw new InvalidOperationException($"Failed to find a runtime containing dbgshim.dll under '{root}'");
    }
}
