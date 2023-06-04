using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Model;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter
{
    private LaunchRequestArguments? launchArgs;
    private AttachRequestArguments? attachArgs;

    public Task<LaunchResponse> LaunchAsync(LaunchRequestArguments args)
    {
        this.launchArgs = args;

        var toRun = args.LaunchAttributes!["program"].GetString()!;
        // TODO: Consider no-debug
        this.debugger = this.debuggerHost.StartProcess("dotnet", toRun);

        this.debugger.OnStandardOut += async (_, args) => await this.client.SendOutputAsync(new()
        {
            Category = OutputEvent.OutputCategory.Stdout,
            Output = args,
        });
        this.debugger.OnExited += async (_, e) => await this.OnDebuggerExited(e);

        this.debugger.OnBreakpoint += async (_, a) => await this.BreakAt(
            a.Thread,
            a.Breakpoint.IsEntryPoint
                ? StoppedEvent.StoppedReason.Entry
                : StoppedEvent.StoppedReason.Breakpoint);
        this.debugger.OnStep += async (_, a) => await this.BreakAt(a.Thread, StoppedEvent.StoppedReason.Pause);

        return Task.FromResult(new LaunchResponse());
    }

    public Task<AttachResponse> AttachAsync(AttachRequestArguments args)
    {
        this.attachArgs = args;
        throw new NotSupportedException("attaching is not yet supported");
    }

    public Task<ThreadsResponse> GetThreadsAsync() =>
        Task.FromResult(new ThreadsResponse()
        {
            // TODO: Hardcoded
            Threads = new Thread[]
            {
                new()
                {
                    Id = 0,
                    Name = "main",
                }
            },
        });

    private async Task OnDebuggerExited(int exitCode)
    {
        await this.client.DebuggerTerminatedAsync(new());
        await this.client.ProcessExitedAsync(new()
        {
            ExitCode = exitCode,
        });
    }
}
