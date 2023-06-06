using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Adapter;
using Draco.Dap.Adapter.Capabilities;
using Draco.Dap.Model;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IConfigurationSequence
{
    public Task<ConfigurationDoneResponse> ConfigurationDoneAsync(ConfigurationDoneArguments args) =>
        Task.FromResult(new ConfigurationDoneResponse());
}
