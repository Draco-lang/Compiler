using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter.Basic;

public interface IBreakpoints
{
    [Request("setBreakpoints", Mutating = true)]
    public Task<SetBreakpointsResponse> SetBreakpointsAsync(SetBreakpointsArguments args);
}
