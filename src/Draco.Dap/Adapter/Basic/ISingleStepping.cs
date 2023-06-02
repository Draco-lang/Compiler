using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter.Basic;

public interface ISingleStepping
{
    [Request("stepIn")]
    public Task<StepInResponse> StepIntoAsync(StepInArguments args);

    [Request("next")]
    public Task<NextResponse> StepOverAsync(NextArguments args);

    [Request("stepOut")]
    public Task<StepOutResponse> StepOutAsync(StepOutArguments args);
}
