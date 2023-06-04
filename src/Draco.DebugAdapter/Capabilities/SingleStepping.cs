using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Model;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter
{
    public Task<StepInResponse> StepIntoAsync(StepInArguments args)
    {
        this.currentThread?.StepInto();
        return Task.FromResult(new StepInResponse());
    }

    public Task<NextResponse> StepOverAsync(NextArguments args)
    {
        this.currentThread?.StepOver();
        return Task.FromResult(new NextResponse());
    }

    public Task<StepOutResponse> StepOutAsync(StepOutArguments args)
    {
        this.currentThread?.StepOut();
        return Task.FromResult(new StepOutResponse());
    }
}
