using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter.Basic;

public interface ISingleStepping
{
    [Request("stepIn")]
    public Task<StepInResponse> StepInto(StepInArguments args);

    [Request("next")]
    public Task<NextResponse> StepOver(NextArguments args);

    [Request("stepOut")]
    public Task<StepOutResponse> StepOut(StepOutArguments args);
}
