using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Draco.Dap.Adapter;
using Draco.Dap.Model;
using Draco.Debugger;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IDebugAdapter
{
    private readonly IDebugClient client;
    private DebuggerHost debuggerHost = null!;
    private Debugger.Debugger debugger = null!;

    public DracoDebugAdapter(IDebugClient client)
    {
        this.client = client;
    }

    public void Dispose() { }

    public Task InitializeAsync(InitializeRequestArguments args)
    {
        var dbgShim = FindDbgShim();
        this.debuggerHost = DebuggerHost.Create(dbgShim);

        return Task.CompletedTask;
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

    public Task<LaunchResponse> LaunchAsync(LaunchRequestArguments args)
    {
        var toRun = args.LaunchAttributes!["program"].GetString()!;
        if (args.NoDebug == true)
        {
            this.debugger = this.debuggerHost.StartProcess("dotnet", toRun);

            this.debugger.OnStandardOut += async (_, args) => await this.client.SendOutputAsync(new()
            {
                Category = OutputEvent.OutputCategory.Stdout,
                Output = args,
            });
            this.debugger.OnExited += async (_, a) =>
            {
                await this.client.ProcessExitedAsync(new()
                {
                    ExitCode = a,
                });
                await this.client.DebuggerTerminatedAsync(new());
            };

            this.debugger.OnBreakpoint += (_, a) => this.debugger.Continue();
        }
        else
        {
            throw new NotSupportedException("debugging is not yet supported");
        }
        return Task.FromResult(new LaunchResponse());
    }

    public Task<SetBreakpointsResponse> SetBreakpointsAsync(SetBreakpointsArguments args) => throw new NotImplementedException();

    public Task<StepInResponse> StepIntoAsync(StepInArguments args) => throw new NotImplementedException();
    public Task<NextResponse> StepOverAsync(NextArguments args) => throw new NotImplementedException();
    public Task<StepOutResponse> StepOutAsync(StepOutArguments args) => throw new NotImplementedException();
}
