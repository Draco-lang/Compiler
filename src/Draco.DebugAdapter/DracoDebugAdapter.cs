using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Draco.Dap.Adapter;
using Draco.Dap.Adapter.Breakpoints;
using Draco.Dap.Model;
using Draco.Debugger;
using Thread = Draco.Dap.Model.Thread;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IDebugAdapter
{
    private readonly IDebugClient client;

    private InitializeRequestArguments clientInfo = null!;
    private DebuggerHost debuggerHost = null!;
    private Debugger.Debugger debugger = null!;

    public DracoDebugAdapter(IDebugClient client)
    {
        this.client = client;
    }

    public void Dispose() { }

    public async Task InitializeAsync(InitializeRequestArguments args)
    {
        await Task.Delay(10000);

        this.clientInfo = args;

        var dbgShim = FindDbgShim();
        this.debuggerHost = DebuggerHost.Create(dbgShim);

        // return Task.CompletedTask;
    }

    // TODO: Temporary
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

    public Task<StepInResponse> StepIntoAsync(StepInArguments args) => throw new NotImplementedException();
    public Task<NextResponse> StepOverAsync(NextArguments args) => throw new NotImplementedException();
    public Task<StepOutResponse> StepOutAsync(StepOutArguments args) => throw new NotImplementedException();
}
