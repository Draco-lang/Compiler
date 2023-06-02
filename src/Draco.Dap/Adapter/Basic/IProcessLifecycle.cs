using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter.Basic;

public interface IProcessLifecycle
{
    [Request("launch", Mutating = true)]
    public Task<LaunchResponse> LaunchAsync(LaunchRequestArguments args);
}
