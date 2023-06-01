using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Adapter;
using Draco.Dap.Model;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IDebugAdapter
{
    private readonly IDebugClient client;

    public DracoDebugAdapter(IDebugClient client)
    {
        this.client = client;
    }

    public void Dispose() { }

    public Task InitializeAsync(InitializeRequestArguments args) => Task.CompletedTask;

    public async Task<LaunchResponse> LaunchAsync(LaunchRequestArguments args)
    {
        var toRun = args.LaunchAttributes!["program"].GetString()!;
        if (args.NoDebug == true)
        {
            var process = new Process()
            {
                StartInfo = new()
                {
                    FileName = "dotnet",
                    Arguments = toRun,
                }
            };
            process.Start();
        }
        else
        {
            throw new NotSupportedException("debugging is not yet supported");
        }
        return new();
    }

    public Task<SetBreakpointsResponse> SetBreakpointsAsync(SetBreakpointsArguments args) => throw new NotImplementedException();

    public Task<StepInResponse> StepIntoAsync(StepInArguments args) => throw new NotImplementedException();
    public Task<NextResponse> StepOverAsync(NextArguments args) => throw new NotImplementedException();
    public Task<StepOutResponse> StepOutAsync(StepOutArguments args) => throw new NotImplementedException();
}
