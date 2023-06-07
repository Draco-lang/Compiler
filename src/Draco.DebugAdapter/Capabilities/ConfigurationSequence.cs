using System.Threading.Tasks;
using Draco.Dap.Adapter.Capabilities;
using Draco.Dap.Model;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IConfigurationSequence
{
    public Task<ConfigurationDoneResponse> ConfigurationDoneAsync(ConfigurationDoneArguments args) =>
        Task.FromResult(new ConfigurationDoneResponse());
}
