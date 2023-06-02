using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter;

internal interface IDebugAdapterLifecycle
{
    [Request("initialize")]
    public Task<Capabilities> InitializeAsync(InitializeRequestArguments args);
}
